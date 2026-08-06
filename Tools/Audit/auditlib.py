#!/usr/bin/env python3
"""Authoritative Jozi Runner append-only audit ledger and reporting utilities."""
from __future__ import annotations

import csv
import hashlib
import json
import os
import re
import sqlite3
import tempfile
import uuid
from datetime import datetime, timezone
from decimal import Decimal
from pathlib import Path
from typing import Any, Iterable, Optional

SCHEMA_VERSION = "1.0"
COST_TYPES = {
    "api-actual-from-usage", "api-estimate", "subscription-estimate",
    "fixed-credit-cost", "free-local-operation", "unknown",
}
SECRET_PATTERNS = [
    re.compile(r"(?i)(authorization\s*[:=]\s*)(?:bearer\s+)?[^\s,;]+"),
    re.compile(r"(?i)((?:api[_-]?key|access[_-]?token|secret|password)\s*[:=]\s*)[^\s,;]+"),
]


def utc_now() -> str:
    return datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")


def redact(value: Optional[str]) -> Optional[str]:
    if value is None:
        return None
    result = value
    for pattern in SECRET_PATTERNS:
        result = pattern.sub(lambda m: (m.group(1) if m.lastindex else "") + "[REDACTED]", result)
    result = re.sub(r"\bsk-[A-Za-z0-9_-]{12,}\b", "[REDACTED]", result)
    return result


def sha256_text(value: str) -> str:
    return hashlib.sha256(value.encode("utf-8")).hexdigest()


def canonical(event: dict[str, Any]) -> str:
    payload = {k: v for k, v in event.items() if k != "recordHash"}
    return json.dumps(payload, sort_keys=True, separators=(",", ":"), ensure_ascii=False, default=str)


def blank_event(**values: Any) -> dict[str, Any]:
    event = {
        "schemaVersion": SCHEMA_VERSION,
        "actionId": values.pop("actionId", str(uuid.uuid4())),
        "correlationId": None, "parentActionId": None, "assetId": None,
        "timestampUtc": utc_now(), "actor": "unknown", "provider": "local",
        "operation": "unknown", "model": None, "authenticationType": None,
        "status": "completed", "approvalRequired": False, "approvalStatus": None,
        "approvedBy": None, "approvalTimestampUtc": None, "startTimestampUtc": None,
        "finishTimestampUtc": None, "durationMilliseconds": None,
        "inputTokens": None, "cachedInputTokens": None, "outputTokens": None,
        "totalTokens": None, "tokenSource": None, "estimatedCost": None,
        "actualCost": None, "currency": None, "costType": "unknown",
        "pricingVersion": None, "meshyBalanceBefore": None, "meshyBalanceAfter": None,
        "meshyCreditsConsumed": None, "estimatedMeshyCredits": None,
        "filesRead": [], "filesCreated": [], "filesModified": [], "commandsExecuted": [],
        "gitCommitBefore": None, "gitCommitAfter": None, "promptHash": None,
        "resultHash": None, "previousRecordHash": None, "recordHash": "",
        "errorCode": None, "redactedErrorMessage": None, "rawEventFilePath": None,
    }
    event.update(values)
    event["redactedErrorMessage"] = redact(event.get("redactedErrorMessage"))
    if event["costType"] not in COST_TYPES:
        raise ValueError(f"Invalid costType: {event['costType']}")
    return event


def read_events(ledger: Path) -> list[dict[str, Any]]:
    if not ledger.exists():
        return []
    events = []
    with ledger.open(encoding="utf-8") as handle:
        for line_no, line in enumerate(handle, 1):
            if not line.strip():
                continue
            try:
                events.append(json.loads(line))
            except json.JSONDecodeError as exc:
                raise ValueError(f"Invalid JSON at ledger line {line_no}: {exc}") from exc
    return events


def append_event(ledger: Path, event: dict[str, Any]) -> dict[str, Any]:
    ledger.parent.mkdir(parents=True, exist_ok=True)
    existing = read_events(ledger)
    previous = existing[-1].get("recordHash") if existing else None
    item = blank_event(**event)
    item["previousRecordHash"] = previous
    item["recordHash"] = sha256_text(canonical(item))
    line = json.dumps(item, sort_keys=True, separators=(",", ":"), ensure_ascii=False, default=str) + "\n"
    # O_APPEND is the authority: never rewrite prior records.
    fd = os.open(ledger, os.O_APPEND | os.O_CREAT | os.O_WRONLY, 0o644)
    try:
        os.write(fd, line.encode("utf-8"))
        os.fsync(fd)
    finally:
        os.close(fd)
    return item


def validate_chain(ledger: Path) -> tuple[bool, Optional[int], str]:
    previous = None
    for index, event in enumerate(read_events(ledger), 1):
        if event.get("previousRecordHash") != previous:
            return False, index, "previousRecordHash does not match prior record"
        expected = sha256_text(canonical(event))
        if event.get("recordHash") != expected:
            return False, index, "recordHash does not match canonical event"
        previous = event["recordHash"]
    return True, None, "valid"


def load_openai_pricing(path: Path, version: Optional[str] = None) -> dict[str, Any]:
    data = json.loads(path.read_text(encoding="utf-8"))
    if version and data["pricingVersion"] != version:
        raise ValueError(f"Pricing version {version} not found in {path}")
    return data


def calculate_openai_cost(model: str, input_tokens: Optional[int], cached_tokens: Optional[int],
                          output_tokens: Optional[int], pricing: dict[str, Any],
                          cost_type: str = "api-estimate") -> Optional[Decimal]:
    if input_tokens is None or cached_tokens is None or output_tokens is None:
        return None
    rates = pricing["ratesPerMillionTokens"].get(model, pricing["ratesPerMillionTokens"].get("default"))
    if not rates or any(rates.get(k) is None for k in ("input", "cachedInput", "output")):
        return None
    million = Decimal(1_000_000)
    return (
        Decimal(input_tokens) * Decimal(str(rates["input"])) / million
        + Decimal(cached_tokens) * Decimal(str(rates["cachedInput"])) / million
        + Decimal(output_tokens) * Decimal(str(rates["output"])) / million
    )


def meshy_credits_consumed(before: Optional[Decimal], after: Optional[Decimal]) -> Optional[Decimal]:
    if before is None or after is None:
        return None
    return max(Decimal(0), before - after)


def budget_decision(used: Decimal, budget: Decimal, policy: dict[str, Any],
                    owner_approved: bool = False) -> dict[str, Any]:
    if budget <= 0 and policy.get("zeroBudgetMeansDisabled", True):
        return {"percentage": None, "level": "disabled", "allowed": True, "ownerApprovalRequired": False}
    percentage = (used / budget) * Decimal(100) if budget > 0 else Decimal(100)
    if percentage >= Decimal(str(policy["hardStopThresholdPercentage"])):
        return {"percentage": percentage, "level": "blocked", "allowed": False, "ownerApprovalRequired": True}
    if percentage >= Decimal(str(policy["approvalThresholdPercentage"])):
        return {"percentage": percentage, "level": "approval", "allowed": owner_approved, "ownerApprovalRequired": True}
    if percentage >= Decimal(str(policy["warningThresholdPercentage"])):
        return {"percentage": percentage, "level": "warning", "allowed": True, "ownerApprovalRequired": False}
    return {"percentage": percentage, "level": "normal", "allowed": True, "ownerApprovalRequired": False}


def generation_allowed(events: Iterable[dict[str, Any]], asset_id: str, maximum: int) -> bool:
    paid_ops = {"CreateTextTo3DTask", "CreateImageTo3DTask", "CreateMultiImageTo3DTask"}
    count = sum(1 for e in events if e.get("assetId") == asset_id and e.get("provider") == "meshy"
                and e.get("operation") in paid_ops and e.get("status") not in {"blocked", "cancelled"})
    return count < maximum


DB_COLUMNS = [
    "actionId", "correlationId", "parentActionId", "assetId", "timestampUtc", "actor",
    "provider", "operation", "model", "status", "approvalStatus", "estimatedCost",
    "actualCost", "currency", "costType", "meshyCreditsConsumed", "recordHash",
]


def project_sqlite(ledger: Path, database: Path) -> int:
    events = read_events(ledger)
    database.parent.mkdir(parents=True, exist_ok=True)
    with sqlite3.connect(database) as db:
        db.execute("DROP TABLE IF EXISTS actions")
        db.execute("""CREATE TABLE actions (
            actionId TEXT PRIMARY KEY, correlationId TEXT, parentActionId TEXT, assetId TEXT,
            timestampUtc TEXT, actor TEXT, provider TEXT, operation TEXT, model TEXT,
            status TEXT, approvalStatus TEXT, estimatedCost TEXT, actualCost TEXT,
            currency TEXT, costType TEXT, meshyCreditsConsumed TEXT, recordHash TEXT NOT NULL)""")
        placeholders = ",".join("?" for _ in DB_COLUMNS)
        db.executemany(f"INSERT INTO actions ({','.join(DB_COLUMNS)}) VALUES ({placeholders})",
                       [[None if e.get(c) is None else str(e.get(c)) for c in DB_COLUMNS] for e in events])
        db.execute("CREATE INDEX idx_actions_timestamp ON actions(timestampUtc)")
        db.execute("CREATE INDEX idx_actions_asset ON actions(assetId)")
        db.execute("CREATE INDEX idx_actions_correlation ON actions(correlationId)")
    return len(events)


def export_csv(events: Iterable[dict[str, Any]], destination: Path) -> int:
    rows = list(events)
    destination.parent.mkdir(parents=True, exist_ok=True)
    fields = sorted({key for row in rows for key in row.keys()}) if rows else DB_COLUMNS
    with destination.open("w", newline="", encoding="utf-8") as handle:
        writer = csv.DictWriter(handle, fieldnames=fields)
        writer.writeheader()
        for row in rows:
            writer.writerow({k: json.dumps(v, separators=(",", ":")) if isinstance(v, (list, dict)) else v for k, v in row.items()})
    return len(rows)
