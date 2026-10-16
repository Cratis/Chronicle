<!-- cratis-ai-managed: skills/cratis-screenplay-model-authoring/references/release-baseline.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Release baseline

## Contents

- Admit in the release job
- Choose the edit mode from evidence
- Inspect and review through MCP
- Fail closed and recover
- Earlier shipped models
- Verified product sources

Requires Screenplay 4.153.0 or later (`screenplay --version`) for the release baseline and its `baseline` command, and 4.152.0 or later for `storedName` (ESM v16).

## Admit in the release job

Commit `.screenplay/release-baseline.json` with the model. It records the event
revisions a release admitted, keyed by persisted event contract identity and
revision, not display name/address. Its header is
`schema: "cratis.screenplay.release-baseline"`, `schemaVersion: 1`, with
`applicationIdentity` and `entries`. Published schema:
[release-baseline-v1.schema.json](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/release-baseline-v1.schema.json).

Persist `.screenplay/identities.json` through the owning MCP session first.
Admission requires a binding model, persisted identities and no pending recovery.
At release time, run the standalone tool explicitly:

```shell
screenplay baseline admit <model-root> --release <release-label>
screenplay baseline check <model-root>
```

Admission freezes every current and historical revision's resolved stored shape.
It adds new entries, retains matching entries' original release labels and never
removes or re-admits changed entries. Building, rendering, applying a proposal or
publishing a package does **not** admit a release. A release job must run admission
and commit the sidecar; otherwise shipped-but-unadmitted revisions remain editable.

`check` exits **0** for matching admitted revisions, **1** for
`BaselinedRevisionChanged` or `BaselinedRevisionMissing` (changed entries report
first differing paths), **2** when checking is unavailable, including unreadable
state. A nonbinding model is not a verified matching set: missing revisions can
still be reported, but other entries are unknown. Admission exits **0** on success,
**1** for changed overlapping revisions and **2** for invalid input or failed writes.
A missing baseline is not an unreadable baseline, and a clean check with no entries
is not evidence that a shipped release has been admitted.

The stored shape includes the effective event name, property names, optionality,
collection flags, resolved primitives/composites, compliance and enum names/numbers.
Changing a shared type or concept can invalidate historical revisions too: create a
new type/concept for a new generation rather than mutate the old shared schema.
Descriptions, tags, type/concept names, semantic property IDs, concept validation
rules and generation strategies are outside stored-shape equality. Baseline edits
change neither source/catalog revisions nor canonical ESM bytes/revision; this is
an independent release gate, not V1, V3, V4 or target migration evidence.

## Choose the edit mode from evidence

| `mutability.basis` | Current event property edit | Event rename |
| --- | --- | --- |
| `noBaseline` | Exactly one of `newGeneration: true` or `eventNeverPersisted: true` is required, even for optional additions; otherwise `EventContractChangeRequiresDecision` | Pins the previous effective stored name by default |
| `notInBaseline` | No flag defaults to `inPlace`; either explicit mode is honored | Does not add a pin |
| `baselined` | Only explicit `newGeneration: true`; otherwise `BaselinedRevisionImmutable`. `eventNeverPersisted` cannot unfreeze it | Preserves the effective stored name; `eventNeverPersisted` refuses |

Without a baseline, `eventNeverPersisted: true` still requires actual storage-history
knowledge; the identity catalog is not that evidence. With a baseline, unadmitted
means **not recorded**, not proof that nothing was deployed. Historical properties
remain uneditable. One mode applies to all explicitly selected events, without
selecting extra events. `property.generations[]` reports each event's own `from`,
`to`, `mode` (`inPlace`, `eventNeverPersisted` or `newGeneration`) and `mutability`.
That object contains `basis`, `releaseBaselineRevision` and `admittedIn`.

If any historical revision is admitted, a logical event rename preserves its pin.
Inspect `eventRename.mutability` and `eventRename.pinAdded`. Differing pins carry
`storedName` in ESM v16; equal-name pins remain no-ops. A baseline does not admit a
renderer or supply transformations between generations: target checks still apply.

Never hand-edit the sidecar except a deliberate **reviewed correction**. Do not
delete entries, change hashes or admit again to bypass a frozen-contract refusal.

## Inspect and review through MCP

Discover installed schemas: `screenplay_list_release_baseline` (catalog name
`list-release-baseline`) requires an open workspace; takes `offset`, `limit`
(default 50, maximum 200) and `expectedReleaseBaselineRevision`, required on
continuation. `open-workspace` / `workspace-state` report `releaseBaseline` with
`status` (`absent`, `valid`, `unreadable`), `revision`, `reason`, `entries`,
`violations` and `checkFailure`. Absent uses `brev1:absent`; unreadable has no revision.

Entry status is `unchanged`, `changed` (first differing `path`), `missing`, or
`unknown` when the model cannot bind. Unreadable returns `page: null`, not a
successful empty set. Restart paging after baseline drift; source revision guards
alone do not protect this evidence.

Review `read-proposal view=release-baseline` separately from `semantic-diff`.
It reports retained baseline status, violations and original revisions' mutability;
pin `expectedReleaseBaselineRevision` on continuation. All proposal tools and
`apply` accept that optional guard. Every proposal checks the complete candidate's
resolved contracts before retention, including indirect shared-type changes. Apply
checks them again and refuses `StaleReleaseBaseline` after baseline drift; re-read
and re-propose rather than retry the old plan. Semantic-diff's disk comparison
baseline is not this release baseline.

## Fail closed and recover

Malformed JSON, merge conflicts, wrong application/schema or invalid schema hashes
never become `noBaseline` or `notInBaseline`. Opening, reading and compilation
remain available, but event-contract changes refuse `UnreadableReleaseBaseline`;
non-contract edits may proceed. MCP never admits or repairs the sidecar.

CLI admission uses the shared `.screenplay/pending.json` journal. A pending journal
blocks admission; inspect/recover the identified operation through the existing
workspace recovery flow, not by deleting journals. A baseline refusal before
installation has no write outcome to recover.

## Earlier shipped models

`screenplay baseline admit <model-root> --release <release-label> --from-esm <shipped-esm>`
strictly reads an earlier canonical ESM with the same application identity. It resolves
each shipped event type from the shipped contract's `StoredName`, falling back to `Name`,
and refuses `ShippedStoredNameMismatch` when that name differs from the current stored
name. Do not remove a correct current pin to force admission. A reviewed baseline
correction or data migration needs the actual stored-type evidence. Ordinary admission
from the current model uses its effective stored name.

Requires Screenplay 4.154.0 or later (`screenplay --version`) for `baseline admit --from-esm <shipped-esm>` to read the
shipped `storedName`; earlier tools compare the shipped `name` and can refuse a pinned
rename with `ShippedStoredNameMismatch`.

## Verified product sources

Cratis/Screenplay:

- `Documentation/screenplay/events.md` (stored shape, modes, rename).
- `Documentation/screenplay/tool.md` (CLI, persistence and exits).
- `Documentation/screenplay/mcp/reference.md` (views, paging, reports and guards).
- `Documentation/screenplay/mcp/recovery.md` (unreadability, drift, journal).
- `Documentation/screenplay/release-baseline-v1.schema.json` (sidecar).
- `Source/DotNET/Tool/BaselineCommand.cs` (real CLI requirements/exits).
- `Source/DotNET/Screenplay/Workspaces/ReleaseBaseline.cs` (monotonic admission, stored-name comparison and the `--from-esm` boundary).
- `WorkspaceEventMutability.cs`, `WorkspaceRefactoring.Property.cs` and `WorkspaceRefactoring.cs` in that Workspaces folder (classification/refusal).
- `Source/DotNET/Screenplay.Mcp/McpWorkspaces.ReleaseBaseline.cs` (read, paging and apply checks).
