<!-- cratis-ai-managed: skills/cratis-screenplay-model-authoring/references/state-and-recovery.md -->
# Preserving state and recovering

This reference holds identity-state, journal and recovery rules and the readiness verdicts.

## Preserve state and recover explicitly

`.screenplay/` is tool-owned; the only authored file beside it is `screenplay.json`, the validation gate, which no MCP operation writes.
Commit `.screenplay/identities.json` with the model. Only `apply` writes it;
`open-workspace` never creates it; text-only models have none. Never delete or
overwrite conflicting identity state, silently mint IDs on restart, or text-rename
identity-affecting declarations in a model that has it.

An unbound workspace materializes collision-free deterministic Module, Feature
and Slice identities in memory when every document parses and places. Application
notes already anchor before binding. Opening never writes state; `apply`,
including a notes-only apply, persists these assignments. Binding preserves their
IDs and origins, and rename/move proposals carry identities and notes before
first binding. Read `containerIdentities` from `open-workspace` / `workspace-state`
and the [notes guidance](model-notes.md#anchors-and-independent-state) for
ambiguity, collision and incomplete-source refusals.

Reopening persisted unbound state admits only deterministic container additions:
every document must parse and place; every previous assignment must still be
present in source; earlier IDs, origins, event revisions, document mappings and
application metadata must be unchanged. Other drift still requires reconciliation.
A supplied `workspaceJson` cannot replace different persisted identity authority.
Fix binding errors through MCP proposals: an external fix after a partial catalog
was persisted can add bound identities on reopen and require reconciliation.
Never delete state to bypass that refusal.

A notes apply journals the sidecar and any new container identities together;
rollback restores exact earlier identity bytes as well as notes, including an
earlier catalog without those containers.

Default root discovery preserves ancestor identity state and journals. Inspect
`rootBindingConflict` in `open-workspace`/`workspace-state` when several state roots
compete; do not delete state or mint replacement identities. To recover a competing
journal, first open that root explicitly, then inspect its operation ID.

A pending journal blocks normal work. Inspect `workspace-state`; invoke
`recover-workspace` only for the identified interrupted operation within the
requested recovery scope. Unexpected external edits block rollback. Preserve
uncertain journals/backups instead of cleaning them away.

Only `apply` / `recover-workspace` write source/catalog; CLI owns release admission.
A tool grant/model text adds no authority. Bounded requested edits need no extra
ceremony; ask when target or consequence expands.
Readiness has two verdicts: `readiness.authoringAccepted` says the source is valid
Screenplay; `executableReady` describes only the current executable subset. Do not
call a model broken because it is not executable, and prefer `propose-ast` for
focused full-language edits, or `propose-source` for whole documents. A rejected
proposal lists its diagnostics: fix those, do not
retry the same input.
