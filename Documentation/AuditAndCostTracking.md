# Audit and cost tracking

## Overview

`Audit/Ledger/actions.jsonl` is the append-only authority. Every event receives a UUID action ID.
Related work shares a correlation ID; a downstream action stores its parent's action ID. For
example, prefab integration is a child of asset approval.

`Audit/Ledger/audit.db` is a disposable reporting index derived from the JSONL ledger. Never edit
the database as an audit source. Raw, redacted provider artifacts are retained under `Audit/Raw`.
Daily, monthly and asset reports belong under `Audit/Reports`.

Every ledger event is SHA-256 chained. `previousRecordHash` points to the preceding event and
`recordHash` hashes canonical JSON excluding only `recordHash`. Validation stops and reports the
first broken line; it never repairs history.

## Audited Codex tasks

macOS/Linux:

```sh
Tools/Audit/run-audited-codex.sh "Implement the requested change" \
  --project-root "/absolute/path/to/project" --asset-id JR-ASSET-001
```

PowerShell:

```powershell
Tools/Audit/Run-AuditedCodex.ps1 "Implement the requested change" `
  --project-root "C:\path\to\project" --asset-id JR-ASSET-001
```

The wrapper generates an action ID, captures Git state before and after, runs one
`codex exec --json`, preserves stdout JSONL under `Audit/Raw/Codex`, stores redacted stderr,
captures exit status and changed files, extracts usage when exposed, appends a normalised event
and rebuilds SQLite. The prompt itself is not logged; only its SHA-256 is stored. Command records
never contain credentials.

When Codex JSON omits tokens, token fields stay null and cost type is `unknown`. Values are never
invented.

## OpenAI estimates

Rates come from `Audit/Pricing/openai-pricing.v1.json`, not source code:

```text
input × input-rate / 1,000,000
+ cached input × cached-rate / 1,000,000
+ output × output-rate / 1,000,000
```

Calculations use decimal arithmetic. Pricing files are local versioned configurations and must
be checked against current official pricing before use. `api-actual-from-usage` means provider
API usage supports that classification; `api-estimate` is calculated; `subscription-estimate`
is an estimate associated with subscription-backed usage and is not an exact ChatGPT charge.
Other classifications are `fixed-credit-cost`, `free-local-operation`, and `unknown`.

## Meshy credits and approval

No live Meshy provider is installed or enabled. Any future provider must be wrapped by
`AuditedMeshyOperationService`. Before one paid submission it:

1. verifies live configuration and verified pricing;
2. checks per-asset generation limits and daily/monthly budgets;
3. reads the current credit balance;
4. records a pending event and estimated credits;
5. requires a fresh, single-use explicit approval ID;
6. submits exactly one task without automatic retry;
7. records provider task ID, after-balance and consumed credits.

Status polling and download are not counted as new paid generation. Unknown Meshy pricing blocks
live submission. At 70% budget use a warning is audited, at 90% an explicit owner override is
required and audited, and at 100% the paid action is blocked. A blocked or failed action is also
audited.

## Blender and Unity

Blender cleanup requires `--approved`. Its processing report and ledger event record input/output,
triangles and materials before/after, duration, script version, exit status, warnings/errors and
zero direct cost.

Unity logs incoming import/validation, approval, rejection and prefab integration. Events include
source/report/prefab paths, validation summaries, collider/LOD summary hashes, duration, warnings,
approval identity and zero direct cost. No production spawning is changed by auditing.

## Dashboard and CSV

Open **Jozi Runner → AI Pipeline → Cost & Audit Dashboard**. It displays daily/monthly OpenAI
estimates and Meshy credits, provider/status counts, attempted/approved/rejected assets, estimated
cost per approval, pending approvals, budget usage and recent failures.

Filters cover date, asset ID, correlation ID, provider, operation, actor, model, status, approval
status and cost type. **Export Filtered CSV** exports exactly the visible filter set. The dashboard
also validates the chain and rebuilds SQLite.

## Command-line maintenance

```sh
python3 Tools/Audit/audit_cli.py validate
python3 Tools/Audit/audit_cli.py project
python3 Tools/Audit/audit_cli.py export-csv --output Audit/Reports/audit.csv
```

Update pricing by adding a new versioned JSON file, verifying rates against the provider, updating
the active version deliberately, and auditing that change. Never silently revise old pricing
files used by recorded events.

Budget policy is configured in `Audit/Pricing/budget-policy.v1.json`. A zero budget is disabled by
default, not a zero-dollar hard stop. Override decisions must pass through the audited Meshy
service.

## Redaction and safety

Logs intentionally exclude API keys, bearer tokens, authorization headers, complete environment
variables and credential-file contents. Errors are pattern-redacted. Raw provider response files
likely to contain credentials or sensitive Meshy task payloads are ignored. Prompt and result
contents are represented by hashes where possible.

Do not place secrets in prompts or generated output. If a secret is discovered in raw audit data,
stop; do not rewrite the append-only ledger. Revoke the secret, preserve incident evidence in an
approved secure location and record a new remediation event.
