<!-- cratis-ai-managed: skills/cratis-screenplay-toolchain/references/model-export-diff-rename.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Export, compare and rename from the CLI

Read this when a script or a builder needs a model's executable bytes, the difference between two model versions, or an identity-preserving rename without an MCP session.
Requires Screenplay 6.1.0 or later (`screenplay --version`).

## Export

`screenplay export [PATH] [--format esm|openapi|ui] [--screens] [--scope <Module>[.<Feature>[.<Slice>]]] [--output <path>]`

- `esm` (the default) writes the executable semantic model, byte-identical to `SemanticModelSerializer.Serialize`, including `schemaVersion` and `revision`. `PATH` defaults to the current folder; without `--output` the bytes go to stdout; an identical existing file is left untouched.
- `--scope` keeps one module, feature or slice plus the application-wide declarations (concepts, types, policies, triggers, event sources, lifecycles, graphs). The excerpt's `revision` identifies the excerpt.

| Exit | Meaning |
| --- | --- |
| 0 | Written |
| 1 | No executable model for the sources, or `--screens` refused |
| 2 | Invalid arguments or scope, or unreadable sources |

### OpenAPI and UI contract formats

Requires Screenplay 6.5.0 or later (`screenplay --version`).

| `--format` | Output | Needs an executable model | Exit |
| --- | --- | --- | --- |
| `openapi` | OpenAPI 3.1 of every command and query | Yes | 1 without one; `--scope` narrows |
| `ui` | Stack-neutral `screenplay-ui/1` JSON: data, actions, navigation, layout, optional Scene layer | No, only sources | 0 even without one (identities and routes are then absent and stderr says so); `--scope` keeps that module, feature or slice's screens |

`--format openapi`: a command is a `POST` with its inputs as the JSON body plus `POST <route>/validate`; a query is a `GET` with the `by` key and filters in the query string, and list queries take `page`, `pageSize`, `sortBy`, `sortDirection`. Observable queries carry `x-screenplay-observable` (open them as a WebSocket). `x-screenplay-authorization` is `anonymous: true` when nothing applies, else `requires` with the policy names; `x-screenplay-policies` lists the policies; each operation has `x-screenplay-kind` (`command`, `query`, `validation`), `x-screenplay-id` and `x-screenplay-slice`. Responses use the `CommandResult` and `QueryResult` envelopes, `info.version` is the executable-model revision, and the same model gives the same bytes.

MCP `read-workspace` `view: "api-description"` (format `openapi-3.1.0`) and `"ui-contract"` (`screenplay-ui/1`) page the decoded bytes like `executable-model` (`limit` 1 to 196608, `expectedRevision`, `expectedModelRevision`). `api-description` reports `available: false` without bytes when there is no executable model; `ui-contract` stays available with `modelRevision: null`.

### Screens in the export

Requires Screenplay 6.8.0 or later (`screenplay --version`). Screens are deferred from the executable model by default (`PLAY0269`, information) and an export keeps its version and bytes. `screenplay export --screens` (ESM format only; with another format it is rejected) admits them as ESM v42 (language and semantic `42.0`, `schemaVersion: 42`); a screen that places an extension component makes it ESM v52 and lists the extensions needed. MCP `read-workspace` with `view: "executable-model"` and `screens: true` does the same (pass it on every page so `modelRevision` stays pinned).

Each slice then carries an optional final `screens` member: `id`, `name`, `title`, optional `description`, ordered `data` (query identity, optional `by`, shown `fields`), `actions` (command identity, `label`, `subject`, every command input as a property identity with optional `from`, optional `when`, optional `then` screen), `navigation` (target screen, `trigger`, optional `by`, `opens`) and a `layout` of `fits` and nested `regions`. Refresh, state, notifications and dialogs are not carried.

A reference the executable model does not hold refuses admission: CLI exit 1 with the reasons on stderr, MCP `ScreensRefused`. A model without an executable model has nothing to admit. In .NET, `ExecutableScreens.Admit(workspace)` admits them and `UiContractBuilder.For(ExecutableSemanticModel)` in `Cratis.Screenplay.Ui` reads them.

## Compare two model versions

`screenplay diff <before> <after> [--format text|json]` over `.play` files or folders. It reports added, removed, renamed and moved elements, member changes, event-contract changes (marked `[contract-breaking]`), enumeration changes, specification-outcome changes, and identity changes with their direct dependants.

- Elements match by persisted identity when both folders hold `.screenplay/identities.json` for the same application; otherwise by address. The report says which.
- `--format json` carries `before` and `after` (path, source revision, persisted identities used, model revision) and a `difference` object.
- Exit 0 compared; exit 2 could not run (including identities of two different applications).
- `--format json` also carries `difference.slices`, the per-slice change summary: `slices` (each with `change`, `changes`, `beforeAddress`, `afterAddress`, `semanticId`, `declarations`, `dependentSlices`; change kinds `added`, `removed`, `renamed`, `moved`, `changed`), `modelWide` (changed declarations no slice owns: modules, features, concepts, types, policies, personas) and `affectedSlices` (unchanged slices that directly reference a changed slice or a model-wide declaration). Renames and moves appear only under identity matching; address matching reports one removed and one added slice. Dependencies are direct, not transitive: rebuild everything when `modelWide` is not empty.
- To compare two commits, `git worktree add` each into its own folder and diff the folders.

## Rename

`screenplay rename <address> <new-name> [<folder>] [--kind <Command|EventContract|ReadModel|...>]` is the scriptable form of MCP `propose-rename` plus `apply`: it repairs references across files, keeps the element's semantic identity, pins a renamed event's stored type name with `id`, and writes (creating on the first rename) `.screenplay/identities.json`. Exit 0 renamed, 1 refused, 2 could not run.

## Trace a value's lineage

Requires Screenplay 6.6.0 or later (`screenplay --version`) for command, event and screen-field addresses and the direction option.

`screenplay lineage <address> [PATH] [--direction upstream|downstream|both] [--format json]` prints the tree MCP `trace-lineage` pages, all at once: `->` is where the value goes, `<-` where it comes from, and a break (missing mapping, ambiguous name, code the trace cannot read) appears in brackets with its reason. `--format json` prints every hop. Exit 0 complete, 1 with breaks, 2 cannot run (for example the address names nothing).

## Persisted identities

`test`, `export`, `diff` and `rename` open a folder with `.screenplay/identities.json` when one exists, so the CLI and the MCP server report the same persisted identities the MCP server uses.

- `export` and `diff` refuse a source that drifted from the identities (`IdentityReconciliationRequired`, `IdentityMappingConflict`). Restore the names, then rename through the CLI or MCP.
- `test` falls back to address-derived ids and never refuses; any printed semantic id is accepted by `--filter`. A single file, a folder without identities or a drifted folder use address ids.
