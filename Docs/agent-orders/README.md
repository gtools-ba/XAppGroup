# Docs / agent-orders

Orders (briefs) you send to an **external AI agent** (e.g. "Coder") go in here.
Each order is a single markdown file describing one piece of work.

## Naming convention
`YYYYMMDD_<short-slug>.md` — e.g. `20261103_wpf-merge-styles.md`

## What an order should contain
1. **Context** — what the project is, where the code lives, what's already there.
2. **The ask** — the concrete deliverable, in plain words.
3. **Constraints** — what must / must not change (files, behavior, build target).
4. **Inputs** — links to files, screenshots, or skills to load first.
5. **Acceptance criteria** — how you (the senior dev manager + designer) will check it.
6. **Handoff** — where the agent's output should land (see `../agent-report/README.md`).

## Related
- `../agent-report/README.md` — where the external agent drops its report.