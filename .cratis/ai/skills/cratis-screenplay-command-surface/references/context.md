<!-- cratis-ai-managed: skills/cratis-screenplay-command-surface/references/context.md -->
# Contexts and context expressions

## Contents

- The four contexts
- The values they carry
- Reaching the context declaratively
- Caller paths and identity details
- Production metadata is not a context expression
- The other expression roots
- Templates and literals
- Escaping

Four contexts, one per job. What each **omits** is as deliberate as what it
carries: a validation rule cannot see the caller's roles, and a policy cannot see
the causation chain.

## The four contexts

```csharp
public record CommandContext(
    dynamic Command, TenantId Tenant, Identity Identity,
    CausedBy CausedBy, Causation Causation, DateTimeOffset Occurred);

public record QueryContext(
    dynamic Arguments, TenantId Tenant, Identity Identity,
    CausedBy CausedBy, Causation Causation, DateTimeOffset Occurred);

public record RuleContext(
    dynamic Artifact, dynamic Value, string Property,
    TenantId Tenant, CausedBy CausedBy, DateTimeOffset Occurred);

public record PolicyContext(
    dynamic Artifact, string Subject, Identity Identity,
    TenantId Tenant, DateTimeOffset Occurred);
```

| Context | In scope for | Omits | Why |
| --- | --- | --- | --- |
| **Command** | a command `handler` | — | the full picture |
| **Query** | a query `performer` | — | same, with `Arguments` instead of `Command` |
| **Rule** | a `validate` rule body | **`Identity`** | a rule *may* reject based on who sent it — "you may not approve your own request" is validation, and `CausedBy` carries the caller's identifier for it. Inspecting **roles or claims** is authorization and belongs in a `policy`; leaving those out is what keeps the two apart |
| **Policy** | a `policy` code block | **`CausedBy`, `Causation`** | a decision about the caller, not an audit record |

`RuleContext` carries `Artifact` (the whole thing under validation), `Value` (the
value the rule is declared on) and `Property` (where it sits — empty for a fenced
`validate` block). A `require` condition is declarative and gets no code context.

Beside every `dynamic` member there is a typed accessor so LINQ binds at compile
time: `CommandAs<T>()`, `ArgumentsAs<T>()`, `ArtifactAs<T>()`, `ValueAs<T>()`, and
`StateAs<T>()`/`EventAs<T>()` on the reducer context. A payload of another type
throws `ContextPayloadTypeMismatch`.

Typed contexts before generation (`CommandValidation`,
`ConceptValidation`, `RulePredicate`, `PolicyPredicate`) expose **input properties
only**. Generated values exist only after authorization/validation, so they are
absent from those shapes. A generated identifier's policy subject is `unavailable`,
not the eventual id; never authorize against it. The handler CommandContext retains
the full command shape, including generated properties. Sidecars belong to the same
compilation/revision as their requirements, not a shape borrowed from another model.
Source: `Documentation/screenplay/context.md`.

## The values they carry

| Type | Members |
| --- | --- |
| `Identity` | `Id`, `Name`, `UserName`, `IsAuthenticated`, `Roles`, `Claims` |
| `Claim` | `Name`, `Value` |
| `CausedBy` | `Subject`, `Name`, `UserName` |
| `Causation` | `Type`, `Occurred`, `Properties` |

`Identity` is the **authorization** view of the caller — what a policy decides on.
`CausedBy` is the **audit** view of the same caller — the three values that travel
with an appended event.

## Reaching the context declaratively

Parsed wherever a mapping source is — `produces` mappings, `seed` values, capture
`append` mappings, query `from` parameters. **Only a narrow subset binds to the
executable model in `produces`:** `$context.occurred` (the occurrence time, not a
guaranteed append time) and the audit identity `$context.identity.id`/`.name`/
`.userName` (equal to `$context.causedBy.subject`/`.name`/`.userName`). Tenant,
roles, claims and causation report `PLAY0268`.

| Path | Yields |
| --- | --- |
| `$context.occurred` | when the command or query was received |
| `$context.tenant` | the tenant |
| `$context.command.<property>` | a command property (in commands) |
| `$context.arguments.<name>` | a query argument (in queries) |
| `$context.identity.id` | the caller's identifier |
| `$context.identity.name` / `.userName` | display name / user name |
| `$context.identity.isAuthenticated` | whether the caller is authenticated |
| `$context.identity.roles` | the caller's roles |
| `$context.identity.claims.<name>` | one claim value |
| `$context.causedBy.subject` / `.name` / `.userName` | the audit view |
| `$context.causation.type` | the causation type |

An unknown `$context.` path is reported, so a typo does not silently become null.

## Caller paths and identity details

Checked: `$identity.<property>` is the same caller value as
`$context.identity.<property>`. Prefer `$identity` in new mappings; the older root
is supported, not deprecated. An older CLI-bundled compiler can reject a top-level `identity` block (`PLAY0001`) and read a
`$identity.<property>` mapping as a raw expression (`PLAY0268`); compiler versions are covered in toolchain `SKILL.md`. Check current renderer
admission separately before choosing caller mappings. Built-ins are `id`, `name`, `userName`,
`isAuthenticated`, `roles`, `claims.<name>` (everything after `claims.` is opaque).
Bare `$identity` reports PLAY0152; unknown properties report PLAY0155. Only
`$identity.id`/`.name`/`.userName` bind in portable `produces` mappings, exactly
like the older forms; roles, authentication status and claims still report PLAY0268.

Requires Screenplay 4.121.0 or later (`screenplay --version`) for `$identity`; before it use `$context.identity`.

A top-level `identity` block declares additional typed caller details:

```screenplay
identity
  description "What the application knows about whoever is calling"
  department String optional from claim "department"
  organization Organization optional from query MyOrganization by $identity.id
module Organizations
  feature Membership
    slice StateView Mine
      readmodel Organization
        name String
      query MyOrganization => Organization optional
        by userId String
```

At most one block per document and assembled folder; layout expansion writes it
to `application.play`. Each unique detail has exactly one source: opaque claim,
keyed single-result query, tagged inline code fence or repository-relative `file`.
It cannot redeclare built-ins. Claim/query sources have no body; refresh/cache
belongs to the runtime. Query sources need `by <expression>` using only token
built-ins, claims or literals, never another detail, input, environment or view.
The query's effective authorization must not depend on details. Result type must
match; an optional query result needs an optional detail, while a required result
may populate an optional detail. Details may themselves be optional or collections.

The block is **authoring metadata**, not runtime source resolution or ESM data.
Declarations alone do not block executable readiness. Executable reads of
`$identity.<detail>` report PLAY0268; `$context.identity` stays built-ins only.
`scoped to` is an opaque scope name, not a detail reference. PLAY0633–0646 diagnose
source/header/type/cardinality errors; there are no automatic identity repairs.
Authority: `identity.md`, `context.md`, `diagnostics.md`.

## Production metadata is not a context expression

`namespace`, `sequence`, `correlation`, `causation`, `causedBy` and `occurred`
are reserved, system-assigned directives in production bodies (`PLAY0476`).
They are not routing knobs. Escape a genuine payload property with `@`, as in
`@sequence String = name` in an inline event. `occurred at` is not available yet.
Reading `$context.occurred` into a payload mapping does not assign the event's
occurrence metadata; use `given clock`, never `given time`, to state scenario time
(the specification skill distinguishes parsing from execution).

## The other expression roots

| Root | Where | Yields |
| --- | --- | --- |
| `$identity.<property>` | caller expression | built-in caller value or declared authoring detail; execution admission is narrower |
| `$env.<VAR_NAME>` | mapping sources | an environment variable |
| `$eventContext.<property>` | **projections only** | `occurred`, `sequenceNumber`, `correlationId`, `eventSourceId` |
| `$eventSourceId` | **projections only** | shorthand for `$eventContext.eventSourceId` |
| `$causedBy.<property>` | **projections only** | `subject`, `name`, `userName`; parses, but does not bind (`PLAY0268`) — write `$eventContext.causedBy.subject` instead |
| `$.` | **captures only** | a value from the current source item |
| `$strings.<dotted.key>` | labels, titles, messages | a localized string from a `.strings` file |

## Templates and literals

A template is backticked with `${}` substitutions. It parses in mappings but does
not bind to the executable model in `produces` or projections. Excerpt, one
mapping line inside a `produces` block:

```screenplay excerpt
fullName = `${firstName} ${lastName}`
```

Literals are `true` / `false`, `"quoted text"`, numbers (`42`, `-3.14`), and
`null`. In a projection, `literal <value>` forces a value to be read as a literal
rather than a property path — that is how a constant key is written. Excerpt,
inside a `projection`:

```screenplay excerpt
from UserLoggedIn key literal "site-stats"
  count totalLogins
```

⚠️ A template expression is **not allowed in a composite key**, and a composite
key with no parts is an error.

## Escaping

A reserved word used as a property name is escaped with a leading `@` —
`@with`, `@file`, `@validate`. This matters most in three places:

- a `trigger` body reserves `file`, so a trigger value named `file` is `@file`;
- an `Enum` concept value named `validate` is `@validate`, or it reads as an
  empty validate block;
- a projection clearing a property named `with` is `clear @with`.
