<!-- cratis-ai-managed: skills/cratis-screenplay-model-authoring/references/repairs.md -->
# Repairing and extracting before rewriting

This reference holds the repair loop, the repairable diagnostics and event extraction guidance.

## Repair or extract before rewriting

Read `read-workspace` with `view: "diagnostics"`, then `view: "repairs"` at the
same revision. Pass the returned `diagnosticCode`, `subject`, required formatting
and both revisions to `propose-repair`; preview with `read-proposal`, then `apply`.
Only verified repairs are listed, and the selected repair gets a fresh transaction.
For PLAY0166/PLAY0478, use the evidence-pinned loop in `references/mcp-tools.md`:
`pinRepairEvidence` plus `expectedRepairEvidenceRevision` prevent attachment drift
from silently changing the reviewed candidate.

- `PLAY0166` declares a missing produced event; `PLAY0478` is a routing decision
  (identifier destination, never fix-all); `PLAY0471` removes a redundant name-equal event pin.
- `PLAY0516` repairs presentation order by moving a sibling or import, or pinning
  a producer before a glob. It preserves semantics/identities and introduces no
  new timeline findings; evidence pinning is unsupported for this repair.
- `PLAY0469` removes an inline identifier payload copy: it **changes the event
  contract**, refuses consumer/opaque impact, has no fix-all, and the catalog does
  not prove events were persisted, so ask about stored contracts first. For a
  contract that may have been stored, use `propose-property` `remove` with
  `newGeneration: true` instead: it drafts a new generation (see
  [property planning](property-links.md#remove-from-an-inline-event)).
  The repair itself never drafts a generation.
- `PLAY0479`: migrate optionality spelling with `PreserveTrivia`; document scope
  migrates all occurrences together. Keep the `query Q => observable?` exception.
- `PLAY0470` is repaired only from an author-supplied destination intent, never
  from a no-input action: read [destination intent repair](destination-intent-repair.md).
- Generation repairs PLAY0683/0684: [subjects, review and limits](mcp-tools.md#concept-generation).
- Compliance repairs PLAY0565/0653: [MCP tool guide](mcp-tools.md).

`propose-number-enum-members` pins an unnumbered enum to current positions.
Opt-in MCP `checks: "enum-pinning"` / `--check enum-pinning` (PLAY0671) hints at
stored enums, not correctness repairs. Arguments/review: `references/mcp-tools.md`.

Use `propose-extract-inline-event` before adding generations (canonical consent;
no comment loss/reverse). The [release baseline](release-baseline.md):
admit in the release job; no baseline requires an explicit mode;
`eventNeverPersisted: true` cannot unfreeze admitted revisions. Differing event pins carry
`storedName` in ESM v16. Pins are event-name identifiers,
shared by all generations and unique (PLAY0472). A renderer admits only the ESM versions it
bundles, so an older Stage can refuse v16. Stored stream `All` (even `id "All"`) and stored source `Default` refuse PLAY0802; rename without
that pin. Evidence/limits: [MCP tool guide](mcp-tools.md).

Requires Screenplay 4.153.0 or later (`screenplay --version`) for the release baseline and its `baseline` command, and 4.152.0 or later for `storedName` (ESM v16).
