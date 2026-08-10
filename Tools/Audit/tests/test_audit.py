import csv
import json
import sqlite3
import sys
import tempfile
import unittest
from decimal import Decimal
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from auditlib import (
    append_event, blank_event, budget_decision, calculate_openai_cost, export_csv,
    generation_allowed, meshy_credits_consumed, project_sqlite, read_events, redact,
    validate_chain,
)


PRICING = {
    "pricingVersion": "test-v1",
    "ratesPerMillionTokens": {
        "test": {"input": 2, "cachedInput": 1, "output": 4},
        "default": {"input": None, "cachedInput": None, "output": None},
    },
}
POLICY = {
    "warningThresholdPercentage": 70,
    "approvalThresholdPercentage": 90,
    "hardStopThresholdPercentage": 100,
    "zeroBudgetMeansDisabled": True,
}


class AuditTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name)
        self.ledger = self.root / "actions.jsonl"

    def tearDown(self):
        self.temp.cleanup()

    def test_openai_cost_calculation(self):
        self.assertEqual(calculate_openai_cost("test", 1_000_000, 0, 1_000_000, PRICING), Decimal("6"))

    def test_cached_token_calculation(self):
        self.assertEqual(calculate_openai_cost("test", 0, 500_000, 0, PRICING), Decimal("0.5"))

    def test_missing_token_data(self):
        self.assertIsNone(calculate_openai_cost("test", None, 0, 1, PRICING))

    def test_meshy_balance_difference(self):
        self.assertEqual(meshy_credits_consumed(Decimal("100"), Decimal("84.5")), Decimal("15.5"))
        self.assertEqual(meshy_credits_consumed(Decimal("84"), Decimal("90")), Decimal("0"))

    def test_budget_warning(self):
        decision = budget_decision(Decimal("70"), Decimal("100"), POLICY)
        self.assertEqual(decision["level"], "warning")
        self.assertTrue(decision["allowed"])

    def test_hard_budget_block(self):
        decision = budget_decision(Decimal("100"), Decimal("100"), POLICY)
        self.assertEqual(decision["level"], "blocked")
        self.assertFalse(decision["allowed"])

    def test_maximum_generation_enforcement(self):
        events = [{"assetId": "A", "provider": "meshy", "operation": "CreateTextTo3DTask", "status": "submitted"}]
        self.assertFalse(generation_allowed(events, "A", 1))
        self.assertTrue(generation_allowed(events, "B", 1))

    def test_jsonl_append_only_behaviour(self):
        first = append_event(self.ledger, {"actor": "test", "operation": "one"})
        original = self.ledger.read_bytes()
        second = append_event(self.ledger, {"actor": "test", "operation": "two"})
        self.assertTrue(self.ledger.read_bytes().startswith(original))
        self.assertEqual(second["previousRecordHash"], first["recordHash"])

    def test_sha256_chain_validation(self):
        append_event(self.ledger, {"actor": "test", "operation": "one"})
        append_event(self.ledger, {"actor": "test", "operation": "two"})
        self.assertEqual(validate_chain(self.ledger), (True, None, "valid"))

    def test_broken_chain_detection(self):
        append_event(self.ledger, {"actor": "test", "operation": "one"})
        append_event(self.ledger, {"actor": "test", "operation": "two"})
        lines = self.ledger.read_text().splitlines()
        item = json.loads(lines[0])
        item["actor"] = "tampered"
        lines[0] = json.dumps(item)
        self.ledger.write_text("\n".join(lines) + "\n")
        valid, line, _ = validate_chain(self.ledger)
        self.assertFalse(valid)
        self.assertEqual(line, 1)

    def test_secret_redaction(self):
        value = redact("Authorization: Bearer abc123 api_key=secret-value sk-abcdefghijklmnop")
        self.assertNotIn("abc123", value)
        self.assertNotIn("secret-value", value)
        self.assertNotIn("sk-abcdefghijklmnop", value)

    def test_sqlite_projection(self):
        append_event(self.ledger, {"actor": "test", "operation": "project"})
        database = self.root / "audit.db"
        self.assertEqual(project_sqlite(self.ledger, database), 1)
        with sqlite3.connect(database) as db:
            self.assertEqual(db.execute("SELECT COUNT(*) FROM actions").fetchone()[0], 1)

    def test_csv_export(self):
        event = append_event(self.ledger, {"actor": "test", "operation": "export"})
        destination = self.root / "events.csv"
        self.assertEqual(export_csv([event], destination), 1)
        with destination.open() as handle:
            self.assertEqual(len(list(csv.DictReader(handle))), 1)

    def test_failed_codex_action_logging(self):
        append_event(self.ledger, {"provider": "openai", "operation": "codex-exec", "status": "failed",
                                   "errorCode": "CODEX_EXIT_1", "redactedErrorMessage": "api_key=hidden"})
        event = read_events(self.ledger)[0]
        self.assertEqual(event["status"], "failed")
        self.assertNotIn("hidden", event["redactedErrorMessage"])

    def test_failed_meshy_action_logging(self):
        append_event(self.ledger, {"provider": "meshy", "operation": "CreateTextTo3DTask", "status": "failed",
                                   "errorCode": "MESHY_SUBMISSION_FAILED"})
        self.assertEqual(read_events(self.ledger)[0]["provider"], "meshy")

    def test_parent_child_correlation(self):
        parent = append_event(self.ledger, {"correlationId": "corr", "operation": "approval"})
        child = append_event(self.ledger, {"correlationId": "corr", "parentActionId": parent["actionId"], "operation": "prefab"})
        self.assertEqual(child["parentActionId"], parent["actionId"])
        self.assertEqual(child["correlationId"], parent["correlationId"])


if __name__ == "__main__":
    unittest.main()
