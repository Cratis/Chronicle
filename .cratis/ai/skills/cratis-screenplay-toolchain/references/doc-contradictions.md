<!-- cratis-ai-managed: skills/cratis-screenplay-toolchain/references/doc-contradictions.md -->
# Documentation contradictions and which side to trust

Current pins: `versions.md`. This is a
**historical** contradiction inventory, not a claim that all listed defects still
exist. Current grammar documents exact numbers and free description placement;
sources/routes bind as ESM v8; PLAY0515 is a source error; CLI admits through
v10 to Stage construct checks. At `v4.135.0:Documentation/screenplay/interoperability.md:133–137`
v11 is still labeled "claimed, unreleased"; Versions.cs admits it and decision
0066 freezes it on package publication. The corpus follows source and 0066.

At v4.127.0 `concepts.md` links the shipped purpose declarations/checks/record
but its final identifier section still calls them later phases. `purposes.md`,
the released grammar/tool and successful purpose probes take precedence. The same
page's legacy `@pii` mentions do not change bare `pii`/`secret` canonical syntax;
legacy forms compile with PLAY0565 information, duplicates with PLAY0653 warning.
Event `subject` remains report-only, despite accepted future lineage work.
Rechecked at 4.142.0: concept `pii`/`personal` and `secret` bind as v13, while
reasons stay PLAY0270 report-only. PLAY0268 at binding only rejects enum/identity
compliance and events mixing personal or explicitly subject-scoped secret values
with an explicit `subject` mark. CLI 3.57.0 bundles Screenplay 4.147.1 / Stage 4.89.1 and
renders v13 concept protection; protected identities/enums remain refused. These are distinct boundaries,
not a reason to remove protection.

`D/` is Screenplay `Documentation/screenplay/` at **v4.66.0**; line numbers were re-read at
that tag. Rule: the compiler you ran decides what the tool accepts today; the documentation
and the domain decide what the model should mean. When they disagree, follow the compiler
for syntax, keep the intended semantics in the model, and record the gap (an issue).
Tool names mean the standalone `screenplay` 4.68.0 and `cratis` 3.28.2 (bundled Screenplay
4.66.0; before 3.28.2 bundled 4.60.1); see `versions.md`. [probed] marks a row re-checked by running a tool; the rest are
read from source or documentation.

| # | Topic | Conflict | Trust |
| --- | --- | --- | --- |
| 0 | ESM v6 | `D/specifications.md` and `D/reactions.md` describe clocks, triggers, captures and reactions as admitted and executed | both tools admit them (ESM v6) since cratis 3.28.2; cratis 3.27.1 (4.60.1) rejected them with PLAY0268 [probed] |
| 1 | `#` comments | PDL docs use `#` (`D/projections/auto-map.mdx:125,147,153`, `D/forms.md:10`) | compiler: PLAY0058 in a projection; use `//` [probed] |
| 2 | projection `automap` | `D/projections/grammar.md:70,77` now allow `automap` for children and nested; the projection-level `ProjDirective` (`:38`) still lists only `no automap` | compiler accepts bare `automap`; keep `no automap` at projection level |
| 3 | AutoMap inside `from` | `D/projections/auto-map.mdx:49` "cannot be toggled" vs `:226` "apply `no automap` per event" | ambiguous; use `no automap` at the projection, children or nested level |
| 4 | child id syntax | `D/projections/keys.md:110` says `id userId` | `identified by` (compiled) |
| 5 | root `remove via join` | `D/projections/removal.md` promises instance removal | `D/projections/semantic-model.md:59,94`: the engine wires it as a child pull and the evaluator does not execute it. Unverified at runtime |
| 6 | `$strings` in authentication | `D/internationalization.md:30` allows it wherever a value expression is accepted | PLAY0183: no provider configuration lines |
| 7 | interaction examples | `D/interactions.md:33` uses `button` | PLAY0103; `on double click` on `table` compiles though not in the EBNF |
| 8 | where screens live | `D/screens.md:3` "inside `StateView` slices" | samples also put dialogs in `StateChange` |
| 9 | where reducers live | `D/slices.md:74` and `D/glossary.md:21` call a reducer an `Automation` thing vs `D/slices.md:87` (`StateView`) | `StateView` (samples) |
| Query rows | Older specification guidance promises authored result-row order | Screenplay v4.145.0 `specifications.md:307-319,426`: compare collections without regard to order, by read-model/key and exact count. Event ordering and collection-valued property ordering are separate |
| 10 | `then` after `when append` | `D/specifications.md:67` "must match exactly the appended fact" vs `:232` and `:282` (v6: what followed it) | v6 text on the standalone tool; before v6, `:67` |
| 11 | constraint collision across sources | `D/constraints.md:118` "cannot yet show" vs `D/specifications.md:77` (`for` lets a spec claim a value on another source) | `specifications.md` (newer) |
| 12 | capture backtick `when` | opaque (`D/captures/grammar.md:321`) vs small portable grammar (`D/captures.md:94`) | both: storage versus binder |
| 13 | clock boundary | resolved: an occurrence fires if due **after** the given clock and **at or before** the when clock (`D/specifications.md:282-284`; `Semantics/Execution/SemanticClock.cs`, `at > from && at <= to`). Given equal to when fires nothing, so the Screenplay sample `Samples/Commerce/Fulfillment/Shipping/ReleasePickingWave.play:20-22` (06:00 to 06:00, expecting `PickingWaveReleased`) is wrong | the code; never write given equal to when. A Screenplay issue for the sample is a candidate |
| 14 | `$causedBy` | not evaluable (#4119), not ESM | showcase only |
| 15 | literal keys | `key literal "global"` vs `key "global"` | both compile; prefer `literal` |
| 16 | query admission | Older `D/queries.md:74` and historical binder probes rejected live/collection/filter queries | Current v4.145.0 `queries.md` admits collection/observable queries and caller-supplied equality filters (ESM v14 since 4.143.0). Context `from`, single-result filters, opaque scope/performer and composite `by` remain PLAY0268 with specific reasons; source/refusal details: read-surface `references/equality-filters.md` |
| 17 | `@pii` | `D/concepts.md:3,33` "Chronicle applies `@pii` and `@sensitive` rules automatically" | the historical binder rejected any compliance attribute (PLAY0268); standalone 4.142.0 admits concept compliance as v13 (since 4.141.0) with narrower enum/identity/subject-lineage refusals (command-surface `references/compliance.md`). Keep protection; the historical pre-v13 CLI reported V3 blocked. `@sensitive` means an operational secret (`[Encrypted]` + `[NotAudited]`; Screenplay decision 0034); the historically probed tools (standalone 4.68.0, bundle 4.66.0) do **not** report `PLAY0515` (`@pii` from 4.69.1, `@sensitive` from 4.84.1): there only Chronicle catches either attribute on an event-source identifier, at build time of the generated code (CHR0034 for `@pii`, CHR0052 for `@sensitive`) |
| 18 | read-model identity | `D/readmodels.md:32` "from the keyed query" | at 4.135.0 explicit `key` wins; else single-instance queries agree on one read-model `by` property; only with none and an admitted list query does conventional `*Id` inference run (collection-key exclusions: read-surface `references/named-keys.md`). Queryless views need explicit `key` |
| 19 | `$strings` constraint messages | `D/constraints.md:31` vs `D/internationalization.md:55` | the compiler keeps the key; realization localizes |
| 20 | composite key braces | optional in the EBNF; both forms in docs | both fine |
| 21 | counters list | `D/projections/index.md:54` lists only increment and decrement | `count` exists too [probed] |
| 22 | ESM version allocation | older text and decisions 0023 and 0024 give exact numbers, operations, streams and reads fixed numbers (v7 to v11) | use actual released binder selection: v7 responses, v8 routing, v9 public boundaries, v10 identity/routes/filters, v11 qualifying retained non-route Double literals at 4.135.0; operations and `numbers exact` source mode remain unadmitted. Never infer current admission from older proposed allocations |
| 23 | claim conditions | the condition grammar lists `==` | claims need `matches` (PLAY0120) [probed] |
| 24 | `numbers exact` | released in 4.64.0 as syntax only, but absent from `D/grammar.md`; documented only in `D/diagnostics.md` (PLAY0508 to PLAY0513) | the compiler: it parses, never binds (PLAY0268, "not admitted by any supported executable model (ESM) version yet"), PLAY0001 on older compilers [probed] |
| 25 | command `concurrency` | `D/commands.md:609` says the executable model "does not bind the `concurrency` block yet" | the binder reports PLAY0271 as an **error**, not a silent skip (`SemanticModelBinder.Commands.cs:32-38`) |
| 26 | MCP approval policy | `D/mcp/reference.md:37` "Keep client approval enabled for both" vs the server instructions (`McpConnection.cs:14`) "hosts need not confirm them each time" | keep approval on; this skill's rule is explicit user approval before `apply` |
| 27 | decision index | `decisions/README.md` row 0020 shows stage `none` while v5 is implemented | the code: v5 `then no readmodel` binds |
| 28 | `visualize-model` kind | the tool filters on the kind "Reactor" (`McpVisualization.cs:21`) although the construct is a Reaction; its added/removed summary never counts reactions (Screenplay#379, open) | the model, not the summary |
| 29 | bodied reducers in Stage | cli `Documentation/reference/screenplay.md:38` says bodied reducers are unsupported (STAGE-ESM-019) | Stage 4.24 code admits pure allowlisted reducer bodies (`PureTransitionAdmission.cs`); trust the code |
| 30 | `cratis screenplay validate <file>` | the command reads as a document validator | file mode ignored the file's imports and reported false unknown-name warnings on CLI 3.28.2 (cli#244, closed by PR #258: later releases compile the file with its imports); validate the folder anyway [probed on 3.28.2] |
| 31 | cli false PLAY0285 | `D/diagnostics.md:640` defines PLAY0285 as a contradiction against every possible producer | cratis before 3.28.2 reported it falsely on reaction cascades (cli#242); not reproduced on cratis 3.28.2 [probed] |
| 32 | competing-claim specification | `D/constraints.md` says a specification "cannot yet show a collision between two event sources" | `D/specifications.md`: `for` on `given` events establishes a claim on one source and attempts the same value on another; the specification page and the tool decide (checked at v4.139.0) |
| 33 | descriptions in rendered code | Stage `Documentation/reference/render-plans.md` says the cratis CLI does not yet call `WithAuthoringMetadata` | the CLI calls it since 3.51.0 (`CratisRenderTarget.cs`): descriptions and documentation render as XML doc comments; `CLI-RENDER-006` when skipped (checked at cli v3.53.1) |

Rows 0 to 12, 14 to 21 and 23 were re-read at the v4.66.0 line numbers above; rows 14, 15,
19, 20 and 23 are properties of the language rather than documentation defects and were
not re-probed on 4.66.0.
