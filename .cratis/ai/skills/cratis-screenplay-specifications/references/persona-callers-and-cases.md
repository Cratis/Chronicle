<!-- cratis-ai-managed: skills/cratis-screenplay-specifications/references/persona-callers-and-cases.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Persona callers and named case tables

## Contents

- Persona callers
- Case tables

Authority: Screenplay v4.127.0 `specifications.md`, `personas.md` and `mcp/reference.md`.

## Persona callers

`given caller as <Persona>` has no body and cannot coexist with an explicit
`given caller`. It expands before binding into an ordinary authenticated caller
satisfying every persona policy; persona names remain outside ESM bytes. Required
atoms are collected first. Policies are visited in declared order, depth first,
left to right: an already satisfied `or` adds nothing, otherwise the leftmost
buildable alternative wins. Operand order matters. This witnesses one minimal
caller, not every caller fitting the persona.

Synthesis refuses no policies, any policy containing `not` (including
`not authenticated`), inline/file policy code, and required non-literal claims.
An `or` may skip an unsupported claim alternative. The role-URI claim type
`http://schemas.microsoft.com/ws/2008/06/identity/claims/role` is never synthesized;
use role atoms, not that claim. Use explicit `given caller` when synthesis is
refused (binding PLAY0573), never infer a runtime allow/deny. Malformed references,
body lines or unknown top-level personas report PLAY0572.

Hover or `declaration-details` (`kind: "Persona"`, `view: "caller"`) shows the
chosen caller and policy contributions or refusal. Opt-in `personas` completeness
checks report personas gating nothing (PLAY0574), gates denying every known
synthesized persona (PLAY0575), and unpinned buildable alternatives (PLAY0576).
Unsynthesizable/undecidable callers are unknown, not denials. A persona-backed
denial still uses `then denied`; a visitor persona is not an anonymous caller.

## Case tables

Two diagnostics to know: `PLAY0588` (warning) for a declared parameter no step references, and `PLAY0585`
(error) when an optional parameter feeds a required target.

Use tables for validation matrices whose steps stay the same. Rows are **cases**,
not examples: `example` means a named typed fixture. This complete model's five
expanded specifications pass in the 4.127.0 reference runner:

```screenplay
policy IsAccountant
  require role "Accountant"
policy IsVisitor
  require role "Visitor"
persona Accountant
  policy IsAccountant
persona Visitor
  policy IsVisitor
module Billing
  feature Invoices
    slice StateChange RecordAmount
      command RecordAmount
        recordId String identifier
        amount Int
        authorize IsAccountant
        validate
          require amount > 0
            message "The amount must be positive"
        produces event AmountRecorded
          amount Int = amount
      specification RecordingAmounts
        description "An accountant can record a positive amount"
        parameter amount Int
        case Small amount = 10
        case Large amount = 100
        given caller as Accountant
        when RecordAmount amount = case.amount
          recordId = "amount-1"
        then AmountRecorded amount = case.amount
      specification RejectingAmounts
        parameter amount Int
        parameter reason String
        case Zero
          amount = 0
          reason = "The amount must be positive"
        case Negative
          amount = -1
          reason = "The amount must be positive"
        given caller as Accountant
        when RecordAmount amount = case.amount
          recordId = "amount-1"
        then error case.reason
      specification RefusingAVisitor
        given caller as Visitor
        when RecordAmount amount = 10
          recordId = "amount-1"
        then denied
```

Every case assigns every typed parameter exactly once, inline or indented.
Values are concrete literals or whole single-line objects/lists, never mappings,
`$` expressions, examples or other case references. `optional` permits `null`,
not omission or a default; ordinary command/event null restrictions still apply.

`case.<parameter>` fills a whole value position, after example expansion and step
overrides. It works in step assignments, generated fixtures, query arguments and
results, `for`, absent read-model keys and scalar `streamId` mappings in a restated
route. Types must match (concept/underlying primitive in either direction is
allowed); optional cannot feed required. `then error case.reason` needs String.
Callers, redelivery locators, clocks, declaration names, step kinds and members
inside structured literals cannot use case values: pass the whole object/list.

Each case expands independently to `<Specification>_<Case>`, in case order,
inheriting the description, with the same executable bytes as hand-expanded
specifications. Derived names must not collide in scope. Failures name the case.
Selecting a table address in `run-specifications` or `screenplay test --filter`
selects all cases; a derived address selects one. MCP specification `cases` pages
effective addresses and values; `find-fixtures` accepts `case` and retains
`table`, `case`, `caseParameter` provenance. C# singular `Expand` refuses tables
(PLAY0589); use `SpecificationExamples.ExpandAll`.
