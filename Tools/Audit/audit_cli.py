#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

from auditlib import export_csv, project_sqlite, read_events, validate_chain


def main() -> int:
    parser = argparse.ArgumentParser(description="Jozi Runner audit ledger utility")
    parser.add_argument("--project-root", type=Path, default=Path(__file__).resolve().parents[2])
    sub = parser.add_subparsers(dest="command", required=True)
    sub.add_parser("validate")
    sub.add_parser("project")
    csv_parser = sub.add_parser("export-csv")
    csv_parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    ledger = args.project_root / "Audit/Ledger/actions.jsonl"
    if args.command == "validate":
        valid, line, message = validate_chain(ledger)
        print(json.dumps({"valid": valid, "firstBrokenLine": line, "message": message}))
        return 0 if valid else 2
    if args.command == "project":
        count = project_sqlite(ledger, args.project_root / "Audit/Ledger/audit.db")
        print(f"Projected {count} events.")
        return 0
    count = export_csv(read_events(ledger), args.output)
    print(f"Exported {count} events to {args.output}.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
