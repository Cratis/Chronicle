<!-- cratis-ai-managed: skills/cratis-screenplay-toolchain/references/language-admission.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Language admission: batches, described bodies, text, reads, dates and composite keys

Select the highest version required by constructs in the model. A newer version
alone never opts into either application mode. Consumers must explicitly admit
exact language/semantic pairs; binding, reference execution and rendering are separate.

| ESM | Selecting construct | Binding/reference behavior | Stage boundary |
| --- | --- | --- | --- |
| v22 | Application-root `reaction batches atomic` | Direct productions per trigger occurrence and each selected refusal branch are separate atomic batches; commands remain independent | v22 admitted with a supporting bundle; atomic mode refused STAGE-ESM-043 |
| v23 | Pending `handler implementation` beside unconditional `produces` | Payload-only body; PLAY0695 information; reaching body gives Unsupported ImplementationBody | v23 admitted, body refused STAGE-IMPL-002 in renderer/Host/executor, even Stub mode |
| v24 | `contains`, `not contains`, `starts with`, `ends with` property/concept rules | Ordinal UTF-16, absent optional passes; not text-condition operators | v24 admitted; semantic Host/executor and generated ordinal comparisons preserve rules |
| v25 | Application-root `command reads protected` | Admitted reads in requirements/mappings, absence Validation, matching decide/commit changes Conflict | v25 admitted; Cratis renderer renders protected reads in a new enough bundle (STAGE-ESM-044 before it, and for shapes it cannot preserve); semantic Host/executor execute reads, non-Error protected requirements refuse STAGE-ESM-045 |
| v26 | Chronological command Date/DateTime comparisons | Same-primitive `==`, `!=`, `>`, `>=`, `<`, `<=`; DateTime UTC ticks and pre-rule offset validation | v26 admitted; Host/executor run it; renderer supports admitted shapes with remaining STAGE-ESM-046 refusals; legacy engines refuse -046 |
| v27 | A read model with two or more `key` marks | Untyped projection `key` blocks, equality-filter lists, single-instance `by` blocks, per-part fixtures | v27 admitted; Host runs it, renderer refuses STAGE-ESM-048/-047, executor refuses specs over it |
| v28 | `==`/`!=` between Uuid values in command `require` and `produces when` | Uuid properties, Uuid concepts and identities, canonical literals; protected read paths in `require` only | v28 refused STAGE-ESM-016 |
| v29 | `decimals <= N`; `add`/`subtract` by `a * b`; a concept mapped to another concept over the same primitive (decision 0098) | Decimal-places rule on a Decimal concept or property; the exact decimal product; the conversion is admitted only when the target declares no rule or compliance the source does not, never for enums, composites or optional into required | v29 refused until Stage admits it |
| v30 | A command `produces ... for <required scalar property>` (several event sources); a specification `given generated <Command>.<property> = <value>` (decision 0098) | Several sources in one accepted command; pinned generated values for reaction-invoked commands, taken in authored order | v30 refused until Stage admits it |
| v31 | A policy `require anonymous` for a public command or query | A deliberately public surface; the policy is the whole effective authorization | v31 refused until Stage admits it |

Constructs of decision 0098 with no executable-model content need no version: application-level toolbar buttons in the layout's `navigation topbar` region (`button <id> title "..." slice <Slice>` with an optional indented `authorize <Policy>`), a command specification that states the public event its outbound Translate slice publishes (no more false `PLAY0285`), and a capture over events that copies a list (`lines = $.lines`) or fans out with `children lines identified by sku`. Check `screenplay --version` against the release that admitted v29 and v30 before relying on them.

Requires Screenplay 4.181.0 or later (`screenplay --version`) for atomic batches; older compilers cannot admit the preamble.

Requires Screenplay 4.182.0 or later (`screenplay --version`) for described handlers; older binders report PLAY0268.

Requires Screenplay 4.184.0 or later (`screenplay --version`) for literal text rules; older binders do not admit those rules.

Requires Screenplay 4.193.0 or later (`screenplay --version`) for protected reads; older binders retain PLAY0271/PLAY0268.

Requires Screenplay 4.199.0 (ESM v26; `screenplay --version`) for date comparisons; older binders report PLAY0268.

Requires Screenplay 4.204.0 (ESM v27; `screenplay --version`) for composite read-model keys; older binders report PLAY0268.

Requires Screenplay 4.205.0 (ESM v28; `screenplay --version`) for Uuid equality in command conditions; older binders report PLAY0268.

Requires Stage 4.124.0 for v24 text-rule admission (`cratis --version` identifies the CLI bundle); older renderers refuse v24 with STAGE-ESM-016. V23 admission alone does not implement described bodies.

Requires Stage 4.127.0 for v25 version admission; older renderers refuse v25 with STAGE-ESM-016. Requires Stage 4.138.0 for the Cratis renderer to render protected reads as Arc read protection; earlier renderers, and reads it cannot preserve, refuse STAGE-ESM-044 and emit no artifacts. `cratis render` gets this from the first CLI that bundles Stage 4.138.0; CLI 3.75.0 bundles Stage 4.137.0.

Requires Stage 4.129.0 for semantic Host/executor protected reads; older engines refuse STAGE-ESM-044. Absence rejects before requirements/generation; read properties feed requirements and productions. Non-Error protected requirements refuse STAGE-ESM-045, including reaction invocations. Legacy Host/structural runner retain -044. Requires Stage 4.142.0 for outside facts to participate in decide/commit, with unrelated appends absorbed; older Hosts fault on any outside append; see [sandbox boundaries](../../cratis-stage-rendering-and-sandbox/references/sandbox-and-specrunner.md#protected-command-reads).

Read [atomic batches and event-source invocation mappings](../../cratis-screenplay-captures-and-reactions/references/atomic-batches-and-source-mappings.md)
for atomic boundaries and the source-identity mapping prerequisite. Start new models
with atomic mode; explicitly review opt-in for legacy models. Keep protection when
Stage refuses: do not remove the declaration or promise a target release date.

Read [described handlers](../../cratis-screenplay-command-surface/references/described-handlers.md)
for the payload result contract and [protected reads](../../cratis-screenplay-command-surface/references/protected-reads.md)
for finite event-source-keyed view admission, single-level paths and repair guards.
Text comparisons and matching are in [text and match rules](../../cratis-screenplay-command-surface/references/text-and-match-rules.md).
`matches` uses the portable linear dialect without selecting a new ESM version;
PLAY0699 refuses nonportable constructs, PLAY0367 invalid syntax, PLAY0787 warns
on exact Studio text-rule patterns and offers a literal-rule repair.

For v26, `require`/`produces when` compare a required scalar command property to
a required same-primitive property or literal. Command/property validation rules
compare only to literals. Date is a calendar date; DateTime is a UTC instant at
seven-fractional-digit tick precision. Participating DateTime inputs without an
offset or `Z` reject with Validation before any rule, even if a comparison is skipped.
`then` matching, keys, constraints and freshness remain ordinal. PLAY0268 retains
mixed Date/DateTime, optional condition operands, `$context` (including occurred),
`today`, protected read paths, concept date rules and reaction `where`. In admitted
positions, invalid date literals or offset-less DateTime literals use PLAY0273.
Read [date comparisons](../../cratis-screenplay-command-surface/references/date-comparisons.md)
for the complete interval example and input/target boundaries.

For v27, read read-surface [named keys](../../cratis-screenplay-read-surface/references/named-keys.md)
for the one complete example and every refusal. Only a model with a read model of two or
more key parts selects v27; one `key` mark keeps its earlier version and bytes. To see the
selected version, open the model with the `screenplay` MCP server and call `read-workspace`
with `view` `executable-model`: `languageVersion` reads `27.0`.

For v28, read command-surface [Uuid equality](../../cratis-screenplay-command-surface/references/uuid-equality.md)
for the operand, literal and refusal rules. Only a command condition that compares Uuids selects v28;
the same condition without Uuid operands keeps its earlier version. To see the selected version, open
the model with the `screenplay` MCP server and call `read-workspace` with `view` `executable-model`:
`languageVersion` reads `28.0`.

Stage's semantic engines admit exact v1–v27 pairs. V28 and mixed
pairs refuse STAGE-ESM-016.
Requires Stage 4.139.0 for v22; older Stage refuses -016.
Admission is not admission of described bodies,
atomic mode or composite-key rendering, and protected reads render only in a bundle that supports them. A CLI may bundle an older compiler/Stage pair: run no-write render
admission rather than transferring a standalone verdict. Pattern diagnostics arise
in Screenplay before rendering; diagnostic arguments add no ESM version or new tool.

Requires Stage 4.135.0 for v26 admission and 4.137.0 for v27; older Stage refuses them with STAGE-ESM-016.
Requires Stage 4.141.0 to render date comparisons; older renderers refuse -046.
Read [rendered dates](../../cratis-stage-rendering-and-sandbox/references/date-comparisons.md)
when checking remaining -046 shapes and input-binding differences.
Requires Stage 4.145.0 for reach-scoped reducer/validation-body refusals
(STAGE-IMPL-003); older engines refuse unrelated behavior too. Described command
handlers retain STAGE-IMPL-002; renderer implementation admission is unchanged.
Read [sandbox boundaries](../../cratis-stage-rendering-and-sandbox/references/sandbox-and-specrunner.md)
when a semantic run reaches a body or uses an authored reaction clock.
