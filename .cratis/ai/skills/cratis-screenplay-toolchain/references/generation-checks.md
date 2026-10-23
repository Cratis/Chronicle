<!-- cratis-ai-managed: skills/cratis-screenplay-toolchain/references/generation-checks.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Generation checks for tool and renderer authors

Use the language-owned APIs instead of copying option tables, rule evaluators or
proof logic. These are .NET APIs of Screenplay, not Cratis C# target contracts.
They never generate a value or execute an attached body.

## Choose the check

| Need | API | Result and boundary |
| --- | --- | --- |
| Populate strategy/option controls | `GenerationStrategyCatalog.All`, `Find(string name)` in `Cratis.Screenplay.Semantics` | Exact case-sensitive source spelling; unknown names return null. Descriptors expose ESM kind, source primitives, introducing version, admission/executability, fixed bits and options with placement, kinds, bounds, choices, defaults and conditions. `rule` serializes as `implementation`; reserved `sequence` is not admitted. Metadata alone is not validation. |
| Check one concept declaration | `SemanticConceptCheck.Check(concept, utcAssessmentYear: 2026)` in `Cratis.Screenplay.Semantics` | Accepts `ConceptSyntax` or `SemanticConcept`; returns `Concept`, `Findings`, `Proofs`, `Support`, `RequiredVersion`. No enclosing application is invented. |
| Check a concrete value | `SemanticValidationRules.Check(concept, value)` in `Cratis.Screenplay.Semantics` | Rule results in declaration order; also single-rule and application/type/value overloads. Not a declaration check or a general type validator. |
| Prove every generated-concept rule | `RuleConsistency.Check(concept, utcAssessmentYear: 2026)` in `Cratis.Screenplay.Semantics.Generation` | Results parallel `concept.Validations`: Proven, Refuted with witness, Unproven with reason. Status is not serialized in ESM. |

Requires Screenplay 4.174.0 (`screenplay --version`) for `RuleConsistency.Check`;
older consumers must not infer proof from missing serialized status.
Requires Screenplay 4.185.0 for `SemanticValidationRules.Check`; older consumers
lack the standalone value-check API.
Requires Screenplay 4.191.0 for `GenerationStrategyCatalog` and
`SemanticConceptCheck.Check`; older tools must use whole-model binding, not a
hand-maintained imitation of admission.

## Check a declaration, not a sample

```csharp
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

var concept = new ConceptSyntax("InviteCode", "String", [], [], SourceLocation.Start)
{
    Generation = new("nanoid", SourceLocation.Start) { Size = 8, Alphabet = "ab" }
};
var check = SemanticConceptCheck.Check(concept, utcAssessmentYear: 2026);
Console.WriteLine(check.Support!.Count); // 256
Console.WriteLine(check.Support.MeetsIdentifierMinimum); // False
```

The assessment year is nullable (null uses the current UTC year); pass it explicitly
for reproducible date support and witnesses. Valid years are 1–9899; invalid years
return PLAY0673 without an assessment. Both overloads accept optional
`assumeGenerated: false`: set true only for an implicit Uuid concept actually used
by a generated property, never to invent generation for String/Int.

Findings carry compiler code, severity, message and construct `Target`, not source
coordinates: a generation-wide finding, an option named by `Option`, or a rule
named by zero-based `RuleIndex`. Syntax indices count authored rules even when
some fail binding; proofs follow the inspected semantic validation array.
Unknown targets are not successful checks. A partial `Concept` is not admission.

Keep `Support.Count` as `BigInteger`; `Bits` is display data and
`MeetsIdentifierMinimum` can be null for unknown support. Unknown entropy is not
zero or proof of sufficient entropy. `RequiredVersion` describes this concept
only. PLAY0683/0684 depend on command-property use and uniqueness, so a concept
check cannot replace whole-application binding. It also proves no authorization,
implementation linkage, uniqueness or runtime readiness.

## Check values and interpret proofs

`SemanticValidationRules.Check` overloads accept `(SemanticValidationRule,
SemanticValue)`, `(SemanticConcept, SemanticValue)` or `(SemanticApplication,
SemanticTypeReference, SemanticValue)`. The last walks supplied composite and
collection values. Pass contract-validated inputs; incompatible rule/value kinds
throw `InvalidSemanticContract` rather than becoming ordinary failed rules.

A `SemanticRuleCheck` carries `Status`, `Rule`, `Message`, optional `Concept` and
`Pattern`. Status is Satisfied, Failed or NotEvaluable: never treat NotEvaluable
as a pass. Absent values satisfy evaluable rules except NotEmpty; opaque rules
remain NotEvaluable. Text compares ordinally, length counts UTF-16 code units,
and numbers compare as exact decimals. Severity does not waive a failed rule.
The API allocates nothing and executes no opaque predicate.

All-Proven rules permit unpinned generation only on targets that support it.
Unproven/Refuted rules need deterministic pins; refuted source contracts must
first be repaired. Passing samples do not prove containment. Stage supports the
all-Proven case from 4.115.0; Screenplay's reference runner and Stage's independent
specification executor still require every reached generated value to be pinned.
A strategy-shape fixture check is separate from checksum arithmetic: author
`validate checksum` to enforce arithmetic on pins too.

## References and verification

Read the product references for complete overload/result contracts:
[concept checks](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/concept-checks.md),
[value-rule checks](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/value-rule-checks.md),
and [strategy catalog and proofs](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/generated-values/strategies.md).
Build a tool against the required Screenplay library; the example's observable
result is count 256 and identifier minimum False. Then bind and verify the whole
model: a declaration check is not an executable-model verdict.
