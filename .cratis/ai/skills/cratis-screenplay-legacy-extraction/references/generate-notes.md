<!-- cratis-ai-managed: skills/cratis-screenplay-legacy-extraction/references/generate-notes.md -->
# `cratis screenplay generate` notes

## Contents

- Command
- Preconditions
- Coverage (what a provider reads)
- Diagnostics are the loss list
- Arc recovery and authoring-only options
- Arc 22.76.0 enum and response recovery
- Arc 22.70.0 compliance recovery
- Generated-document self-check on compiler upgrades
- Reading the output cheaply

Flags verified with `cratis screenplay generate --help` on the version recorded in
cratis-screenplay-toolchain `references/versions.md`; behaviour from the cratis CLI
(`v3.28.2:Source/Cli/Commands/Screenplay/`; the generate files are unchanged since `v3.27.1`), Arc `v22.50.5`
(`Documentation/backend/csharp/generating-a-screenplay.md`,
`Source/DotNET/Screenplay/ScreenplayDiagnosticCodes.cs`), Screenplay.Generation `v0.18.0` and
Screenplay.CritterStack `v0.24.0` (the versions cli 3.53.1 bundles). Later releases exist (Generation 0.18.1 preserves compiler warnings, including on successful generation; CritterStack 0.26.0 adds enum concepts, command-to-event production links and the `MixedSliceKindsSplit` and `ProductionLinkOmitted` diagnostics); do not claim them for `cratis screenplay generate` until a cli bundles them. Version-specific capability facts belong in cratis-screenplay-toolchain
`references/versions.md`.

For the current standalone compiler and CLI bundle, use the toolchain's
[version table](../../cratis-screenplay-toolchain/references/versions.md).
The compliance and self-check sections below are source-checked at Arc 22.70.0
(`Documentation/backend/csharp/generating-a-screenplay.md`,
`Analysis/Types/{ConceptRegistry,PersonalDataMarkings,TypesTheDocumentCannotName}.cs`,
`Emission/Concepts/ConceptSyntaxBuilder.cs`, `Emission/ProtectedIdentityAnnotations.cs`
and `Verification/ExpectedBindingDiagnostics.cs` under `Source/DotNET/Screenplay/`).
They require that generator release, not an older bundled adapter;
a standalone compiler upgrade does not upgrade the generator.

Option/admission evidence checked at CLI 3.41.0: released
`GenerateScreenplaySettings.cs`, `ArcScreenplayGeneration.cs`, v7 planning specs
and `Documentation/reference/screenplay.md`; older diagnostic probes below keep
their original labels. Arc#3054 and CLI#270 describe the recovered-source expansion.

## Command
```
cratis screenplay generate <solution|project|folder> \
  --file .ai-work/screenplay/<model-slug>/legacy/static/static.play [--provider auto|arc|marten|critter-stack] \
  [--framework <TFM>] [--domain <Name>] [--module <Name>] [--feature-root <path>] \
  [--skip-segments <n>] [--modules-from-namespace-roots] -o json
```
- Always pass `--file` into `.ai-work/`. The option help promises a default `Screenplay.play`,
  but the command's own description says it writes to standard output when `--file` is absent
  (`GenerateScreenplayCommand.cs`); the two disagree, so do not rely on either.
- `--file` writes even a partial document when generation reports errors; stdout writes
  nothing on error.
- `--feature-root` is Marten/Critter Stack only; `--modules-from-namespace-roots` is Arc only
  (the other providers warn `CLI0014`).
- Run from the target repository's own checkout. `CLI0017` means workspace source paths that
  cannot be represented as safe, stable identities; fix the location (use the real checkout,
  not a copy or symlink elsewhere) instead of working around it.

## Preconditions
- **Trust and location.** `generate` (and `cratis screenplay conform`) does not start the
  application or connect to its databases, but MSBuild evaluates the targeted project: a
  design-time build that can execute project targets and analyzers and writes intermediate files
  into the project's `obj/`, even when it was never built. Use a clone or worktree you may write
  to, only for source the user trusts, never the live legacy checkout, and name the target in the
  approval. The "run from the target repository's own checkout" note above means a real
  checkout (not a symlinked copy), not the user's working copy.
- .NET SDK and **restored** packages (`CLI0005` otherwise). Run `dotnet restore` only when it
  is the repository's normal workflow; it may touch the network and lock files.
- Target resolution walks up to `.slnx` > `.sln` > `.slnf` > `.csproj`. Spec projects are
  excluded by the last segment of the project name (`Specs`, `Specifications`, `Tests`, `Test`,
  `IntegrationTests`, `Specs.AppHost`; `ScreenplayProjectSelection.cs`).
- Multi-targeted projects need `--framework` (`CLI0015/16`). Provider detection: none found
  `CLI0010`, several `CLI0011`; Arc needs a single host (`CLI0009`).
- Arc analyzes a compilation that has errors and reports `SP0024` (source did not compile, how
  many errors, how many artifacts came through anyway); treat such output as partial. Get the
  source to compile first when you can, and record `SP0024` as a loss row when you cannot.

## Coverage (what a provider reads)
| Provider | Reads | Does not read |
|---|---|---|
| Arc | commands, events, concepts, read models, projections, reducers, reactors, constraints, declarative FluentValidation rules, authorization policies, `produces` from constants and input paths, Chronicle integration specs as given/when/then, `.tsx` beside a slice as `screen` + `data via query` | handler bodies, rules written in code, computed messages (`SP0016`), screen bodies, captures, personas, seeds, `secret`. A rule held to `When`/`Unless` is written down as if unconditional and `SP0016` names the condition: read the diagnostic, do not trust the declaration |
| Marten / Critter Stack (preview; an independent optional adapter built on the neutral Screenplay.Generation SDK, not affiliated with JasperFx: review every `GEN`/`MARTEN`/`WOLVERINE` diagnostic by hand) | aggregates, snapshots, HTTP and message handlers, document operations, queries, outgoing and delayed messages, saga facts | saga lifecycle, policies, middleware bodies, runtime tenancy, upcasting; unbounded extension code is reported by presence only |
| any non-.NET stack | nothing | use Prologue and targeted reads |

## Diagnostics are the loss list
- Families: `SP####` (Arc), `MARTEN####`, `WOLVERINE####`, `VOG####`, `GEN####`,
  `DOTNETSP####` (placement), `CLI####` (host). Each one is an evidence row or a loss-report
  line.
- Every recovered fact ends in one disposition: Lowered, ProvenanceOnly,
  OmittedWithDiagnostic, Conflicted. Anything not Lowered must be resolved (a targeted read or
  an expert question) or explicitly waived with a reason.
- Evidence strength: Exact, Configured, Conventional, Heuristic. Heuristics never establish
  persistence, stream ownership or authorization.
- "Success" only means no error diagnostic was produced. Read warnings and information too.

## Arc recovery and authoring-only options

`--authoring-only-constructs` maps to `ScreenplayOptions.AuthoringOnlyConstructs`
in the Arc adapter (false by default). It includes operations/systems, source and
stream declarations/routes, and reads/requirements recovered from Provide/Handle.
Other providers warn CLI0014 and do not apply it. Added syntax does not realize
code or waive admission: operations refuse PLAY0268 and legacy reads PLAY0271.
Source/stream routes themselves bind as v8 on the current standalone compiler;
older blanket authoring-only route caveats are not its current disposition.

The expanded Arc recovery surface includes:
- Proven command identifiers and event `for` destinations.
- Inline events where one unconditional complete production uniquely owns them.
- Event description/documentation and generation-1 stored-name rename pins.
- Unconditional named Must(Method) rules with implementation-file provenance,
  where no specification exercises the command.
- Event scenarios as `when append`, and event-source-faithful `for` fixtures.
  Scenarios whose sources cannot be stated faithfully are omitted with SP0039;
  record the omission as a loss, never fabricate identities to retain a scenario.
- Supported generated UUID command values and scalar/record responses by default,
  without the authoring-only flag, for commands without successful scenarios.
  Commands with successful scenarios retain the legacy representation until
  deterministic generation fixtures and response expectations can be recovered.
  Unsupported shapes/pre-generation protection remain code with SP0052.

Generated values/responses select ESM v7. The historical CLI 3.41.0 check
admitted v7 to planning, but Stage 4.51.3 refused those members with STAGE-ESM-028/029. CLI#261 is closed; do not say
the compiler or CLI version gate universally refuses v7. Above v7 remains
CLI-RENDER-004. A generated source model is evidence, not guaranteed render output.

Handler/reaction bodies, unresolved conditions and unproven origins still need
source review. Do not infer generation/evolution/absence-assertion coverage from
a package upgrade; read diagnostics and reconcile each recovered scenario.

## Arc 22.76.0 enum and response recovery

Source-verified at Arc `v22.76.0:Source/DotNET/Screenplay/Emission/Concepts/ConceptSyntaxBuilder.cs`,
`Analysis/Types/{ConceptRegistry,FlagsEnumUses}.cs`, `Analysis/EnumConstants.cs`,
`Analysis/Specifications/SpecificationReturnValues.cs`, and
`Documentation/backend/csharp/generating-a-screenplay.md`. Arc 22.76.0 takes
Screenplay 4.139.0, not 4.141.0. The CLI bundles an older generator; see the
central toolchain `references/versions.md` rather than assuming parity.

Every enum member now emits its stored integer, including positional members.
Sparse, negative and reordered values preserve identity when representable as int32
and permitted by `MaximumExecutableModelVersion` (non-positional values need v12).
SP0068 warns when numbers fall back together to positions because of the range or
cap, and on dropped emitted-name collisions: do not treat that output as preserving
stored identity. SP0069 reports aliases sharing a number; the ordinal-first member
used by scenario constant resolution is kept. SP0067 reports `[Flags]` enums with
unnamed combinations and names the property uses; named members still emit, but
unnamed combinations cannot become scenario values. Read every warning as a loss.

Since 22.75.2, `result!` and `in` arguments remain reads for recovered `then returns`.
Writes through any member/element of the executed result (assignment, increment,
decrement, `ref`/`out`, including null-conditional chains) or aliases of the result
or non-string reference values reached through it withhold the response assertion.
This includes variables/fields/initializers, foreach/pattern variables,
returned/yielded/lambda values and conditional/coalescing/tuple/collection flows.
Mutation through ordinary method calls is not analyzed. Inspect reported scenarios;
never infer a response contract from a post-execution mutation.

## Arc 22.70.0 compliance recovery

A concept with both `[Encrypted]` and `[NotAudited]` becomes `secret`.
Encryption or audit suppression alone, or only on a member, does not establish
that concept-wide contract: SP0058 (Information) reports the partial marking
without adding `secret`; an existing `pii` classification remains.

- Subject scope, omitted or explicit, stays bare `secret`.
- `[Encrypted(EncryptionScope.Namespace)]` plus `[NotAudited]` on the concept
  emits bare `secret` by default and SP0061 (Information) names the unstated
  `namespace` scope: the document instead implies Subject scope. Global behaves
  the same way. Set `ScreenplayOptions.AuthoringOnlyConstructs = true` (CLI
  adapter option `--authoring-only-constructs`) to emit `secret scope namespace`
  or `secret scope global` in the concept body. This was syntax-only on that
  compiler; standalone 4.142.0 admits concept compliance as ESM v13 (since 4.141.0).
  That upgrade does not change the older generator or CLI bundle.
- A concept also marked `[PII]` becomes `pii secret`, with no emitted scope:
  the C# providers render only `[PII]`; Chronicle rejects `[PII]` combined with
  `[Encrypted]`. Do not add a scope the provider ignores.

Member `[PII]` is read from properties and matching positional constructor
parameters. Two losses that previously needed manual detection now have
Information diagnostics:

| Code | What the document says or loses | Resolve in source or the intentional candidate |
| --- | --- | --- |
| SP0062 | `[PII]` on a non-concept member (for example, a String) is not stated; the diagnostic names `Type.Member`. No concept is invented. | Introduce a personal-data concept with evidence-backed meaning; keep the original protection and record the lost marking until resolved. |
| SP0063 | A member marking promotes its concept to `pii` everywhere; the diagnostic lists unmarked analyzed uses that acquire that classification. | Review every listed use. If all carry personal data, make the concept-wide classification explicit; if meanings differ, separate the concepts rather than remove `pii`. |

SP0063 is absent when the concept itself has `[PII]` or every analyzed use is
marked. Its list covers analyzed uses, not proof of all runtime use sites.
Reasons, special/criminal qualifiers and processing purposes are not inferred.

SP0059 (Information) reports that an identity concept's `pii/secret` annotations
were left off because Screenplay rejects protected identifiers, destinations and
stream ids (PLAY0515). The identity/route is retained, not the protection. Record
that loss and design a surrogate identity; never treat the generated omission as
authority to remove source protection. SP0058/SP0059 now say `secret` and
`pii/secret`, not the deprecated `sensitive`/`@pii` vocabulary.
See command-surface [compliance](../../cratis-screenplay-command-surface/references/compliance.md)
for classification and persisted-event scope changes.

## Generated-document self-check on compiler upgrades

Arc compiles and binds its printed document. `ExpectedBindingDiagnostics` tolerates
only exact known admission-limit messages (PLAY0268) and the permitted legacy
consistency messages (PLAY0271), not every diagnostic with those codes.
The loaded compiler's contract must list the code as neither retired nor reserved
and include Error among its supported severities. A missing, retired, reserved or
non-error-only code loses the exemption even if an old message still matches.
Changed or unknown messages also fail closed.

After upgrading Screenplay, rerun generation and inspect the self-check instead
of carrying forward an old waiver. Unexpected binding errors become SP0056
(Warning for standalone generation, Information for embedded generation); Arc's
end-to-end gate rejects any SP0056 regardless of severity. Resolve a changed
admission contract or report a generator defect; do not strip protection or widen
the exemption. This check is distinct from a clean source-validation result and
does not prove the model renders or executes.

## Reading the output cheaply
1. V1 on `.ai-work/screenplay/<model-slug>/legacy/static` (folder mode) with the tool and
   command `cratis-screenplay-toolchain` selects, named with its version; record counts, do
   not fix.
2. Use the Screenplay MCP summary on that folder (`describe-application view=summary`),
   then `search-declarations` for the rows you are resolving.
3. Read the generated `.play` per module, never whole when large. It stands in for the code
   only for what the provider covers (coverage table above).
