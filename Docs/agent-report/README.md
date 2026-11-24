# Docs / agent-report

Where the **external AI agent** (e.g. "Coder") drops its report after an order.

## How it works
1. You give the agent an order from `../agent-orders/`.
2. The agent writes its report here.
3. You (senior dev manager + designer) review it against the order's acceptance criteria.

## Naming convention
`YYYYMMDD_<short-slug>.md` — e.g. `20261103_wpf-merge-styles.md`

## Report template
The agent should fill this in:

```markdown
# Report: <short-slug>
Order: <path to the order file>
Date: <YYYY-MM-DD>

## What was done
- <bullet list of concrete changes / deliverables>

## Files changed
- <path> — <one-line what changed>

## How to verify
- <build / run / test commands>

## Open questions / decisions needed
- <anything the agent couldn't decide on its own>

## Notes
- <risks, assumptions, things to double-check>
```

## Status folders (optional)
- `pending/` — reports awaiting your review.
- `reviewed/` — reviewed and accepted/rejected. Move files here once you've checked them.