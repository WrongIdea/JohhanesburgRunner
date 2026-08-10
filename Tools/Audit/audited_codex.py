#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import os
import subprocess
import sys
import time
import uuid
from pathlib import Path
from typing import Any, Optional

from auditlib import (
    append_event, calculate_openai_cost, load_openai_pricing, project_sqlite,
    redact, sha256_text, utc_now,
)


def git(root: Path, *args: str) -> str:
    result = subprocess.run(["git", *args], cwd=root, capture_output=True, text=True)
    return result.stdout.strip() if result.returncode == 0 else ""


def usage_from_stream(path: Path) -> tuple[Optional[str], Optional[int], Optional[int], Optional[int]]:
    model = None
    input_tokens = cached_tokens = output_tokens = None
    for line in path.read_text(encoding="utf-8", errors="replace").splitlines():
        try:
            item = json.loads(line)
        except json.JSONDecodeError:
            continue
        stack: list[Any] = [item]
        while stack:
            value = stack.pop()
            if isinstance(value, dict):
                model = value.get("model", model)
                usage = value.get("usage")
                if isinstance(usage, dict):
                    input_tokens = usage.get("input_tokens", usage.get("inputTokens", input_tokens))
                    cached_tokens = usage.get("cached_input_tokens", usage.get("cachedInputTokens", cached_tokens))
                    output_tokens = usage.get("output_tokens", usage.get("outputTokens", output_tokens))
                stack.extend(value.values())
            elif isinstance(value, list):
                stack.extend(value)
    return model, as_int(input_tokens), as_int(cached_tokens), as_int(output_tokens)


def as_int(value: Any) -> Optional[int]:
    try:
        return int(value) if value is not None else None
    except (TypeError, ValueError):
        return None


def main() -> int:
    parser = argparse.ArgumentParser(description="Run one audited Codex JSON task")
    parser.add_argument("prompt", help="Prompt sent to Codex; only its SHA-256 is written to the ledger.")
    parser.add_argument("--project-root", type=Path, default=Path.cwd())
    parser.add_argument("--model")
    parser.add_argument("--asset-id")
    parser.add_argument("--correlation-id")
    parser.add_argument("--parent-action-id")
    parser.add_argument("--actor", default=os.environ.get("USER", "local-user"))
    args, extra = parser.parse_known_args()
    root = args.project_root.resolve()
    action_id = str(uuid.uuid4())
    correlation_id = args.correlation_id or action_id
    raw_dir = root / "Audit/Raw/Codex"
    raw_dir.mkdir(parents=True, exist_ok=True)
    raw_stdout = raw_dir / f"{action_id}.jsonl"
    raw_stderr = raw_dir / f"{action_id}.stderr.log"
    ledger = root / "Audit/Ledger/actions.jsonl"
    before_commit_sha = git(root, "rev-parse", "HEAD") or None
    before_status = git(root, "status", "--porcelain")
    before_commit = before_commit_sha if not before_status else f"{before_commit_sha}:dirty:{sha256_text(before_status)}"
    before_files = set(before_status.splitlines())
    command = ["codex", "exec", "--json"]
    if args.model:
        command += ["--model", args.model]
    command += extra + [args.prompt]
    started = utc_now()
    start_clock = time.monotonic()
    error_message = None
    try:
        with raw_stdout.open("w", encoding="utf-8") as stdout_handle:
            process = subprocess.run(command, cwd=root, stdout=stdout_handle, stderr=subprocess.PIPE, text=True)
        exit_code = process.returncode
        raw_stderr.write_text(redact(process.stderr) or "", encoding="utf-8")
    except Exception as exc:
        exit_code = 127
        error_message = redact(str(exc))
        raw_stdout.touch()
        raw_stderr.write_text(error_message or "", encoding="utf-8")
    finished = utc_now()
    after_commit_sha = git(root, "rev-parse", "HEAD") or None
    after_status = git(root, "status", "--porcelain")
    after_commit = after_commit_sha if not after_status else f"{after_commit_sha}:dirty:{sha256_text(after_status)}"
    after_lines = set(after_status.splitlines())
    changed = sorted({line[3:] for line in before_files.symmetric_difference(after_lines) if len(line) > 3})
    tracked = set(git(root, "ls-files").splitlines())
    created = [path for path in changed if path not in tracked]
    modified = [path for path in changed if path in tracked]
    model, input_tokens, cached_tokens, output_tokens = usage_from_stream(raw_stdout)
    model = args.model or model
    pricing = load_openai_pricing(root / "Audit/Pricing/openai-pricing.v1.json")
    estimate = calculate_openai_cost(model or "default", input_tokens, cached_tokens, output_tokens, pricing)
    tokens_available = all(v is not None for v in (input_tokens, cached_tokens, output_tokens))
    event = {
        "actionId": action_id, "correlationId": correlation_id,
        "parentActionId": args.parent_action_id, "assetId": args.asset_id,
        "actor": args.actor, "provider": "openai", "operation": "codex-exec",
        "model": model, "authenticationType": "unknown", "status": "completed" if exit_code == 0 else "failed",
        "startTimestampUtc": started, "finishTimestampUtc": finished,
        "durationMilliseconds": round((time.monotonic() - start_clock) * 1000),
        "inputTokens": input_tokens, "cachedInputTokens": cached_tokens, "outputTokens": output_tokens,
        "totalTokens": sum(v for v in (input_tokens, cached_tokens, output_tokens) if v is not None) if tokens_available else None,
        "tokenSource": "codex-exec-json" if tokens_available else None,
        "estimatedCost": float(estimate) if estimate is not None else None,
        "currency": "USD", "costType": "subscription-estimate" if tokens_available else "unknown",
        "pricingVersion": pricing["pricingVersion"],
        "filesCreated": created, "filesModified": modified,
        "commandsExecuted": ["codex exec --json [PROMPT REDACTED; HASH RECORDED]"],
        "gitCommitBefore": before_commit, "gitCommitAfter": after_commit,
        "promptHash": sha256_text(args.prompt),
        "resultHash": sha256_text(raw_stdout.read_text(encoding="utf-8", errors="replace")),
        "errorCode": None if exit_code == 0 else f"CODEX_EXIT_{exit_code}",
        "redactedErrorMessage": error_message or (raw_stderr.read_text(encoding="utf-8")[-2000:] if exit_code else None),
        "rawEventFilePath": str(raw_stdout.relative_to(root)),
    }
    append_event(ledger, event)
    project_sqlite(ledger, root / "Audit/Ledger/audit.db")
    print(json.dumps({"actionId": action_id, "exitCode": exit_code, "raw": str(raw_stdout)}))
    return exit_code


if __name__ == "__main__":
    sys.exit(main())
