---
name: cratis-screenplay-availability
description: Use when a Screenplay model must decide whether a module, feature or slice exists for a tenant, user or system, or when it provides ambient `$context` values. Not for who may act; use cratis-screenplay-command-surface for `authorize`.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-screenplay-availability/SKILL.md -->

# Availability, audiences and context values

Read [preferred syntax](../cratis-screenplay-toolchain/references/preferred-syntax.md) when writing or reviewing `.play`.
Product facts come from Screenplay `availability.md`, `context.md`, `personas.md`
and `policies.md` under `Documentation/screenplay/`; run examples at the pinned
compiler before relying on them.

## Pick the right mechanism

| Question | Use |
| --- | --- |
| Is this part there at all for this tenant, user or system (a package, a rollout, an incident switch)? | `audience`, `availability`, `available` |
| May this caller do it, given who they are and what they can prove? | `policy` and `authorize` ([command surface](../cratis-screenplay-command-surface/SKILL.md)) |
| Is this input acceptable? | `rule` and validation on the command |
| Does a gate or code need a value that is not in the request (the vessel viewed, the project worked in)? | `provide` and `$context.<name>` |
| Which tenant-level commercial module is bought? | Box Office module entitlements; outside the model |

Availability is decided **before** `authorize`. A part that is not available is refused as
`NotAvailable`, which a client can tell apart from not allowed, so it can hide an entry or
explain the package. Do not model a package, rollout or kill switch as a policy, and do not
model a role check as an audience.

## The forms

```screenplay excerpt
audience Plus
  description "Customers on the Plus commercial package"

audience Preview
  description "Tenants taking part in the preview"

availability DataQuality
  description "Sold with Plus, once the vessel has reported"
  for Plus
  when $context.vessel.hasReported == true

module Fleet
  feature DataQuality
    available with DataQuality

    slice StateView QualityScores
      available for Plus or Preview
```

- `audience <Name>` is a named group with a `description` and nothing else. The model
  never says who is in it; membership is granted at runtime, so nothing ships as an accidental entitlement.
- `availability <Name>` is a reusable rule. Its `for`, `when` and `with` lines must all hold;
  `with` builds on another availability. A `with` cycle is PLAY1137.
- `available for <audiences>`, `available when <condition>`, `available with <Availability>`.
  `for` and `with` combine names with `and`, `or` and parentheses. A `when` is one condition with its own
  `and`/`or`. One line holds one form; to mix forms, name them in an `availability` and use `with`.
  Several `available` lines on one construct all have to hold.
- A gate is allowed on a module, feature, slice, command, query and screen. Gates AND down the
  path from module to construct. Gate at the highest level that is true for everything beneath it.
- The runtime context carries `$context.audiences`, the union of the tenant's, user's and
  system caller's audiences. Withdrawing a grant removes it from its own level only.

## Rules the language holds

- **Unknown never opens a gate.** A missing audience, a missing value, an unresolved condition or an
  undecided result leaves the part unavailable. An undeclared audience or availability is a warning
  (PLAY1135, PLAY1136), not an error, and the gate stays closed.
- **Failure is not denial.** A lookup that errors fails the request as a retryable error; it is never
  reported as `NotAvailable`.
- **Specifications always run with everything available.** They verify behavior, not entitlement. There is
  no `given availability` form; do not invent one, and do not write a specification to prove a gate.
- **Events and observers are unaffected.** A reaction that produces into an unavailable slice is fine and
  projections keep running, so switching a part on needs no replay. A reaction is never gated and runs with
  the tenant's and system's audiences, never a user's.
- **Persona `audience` lines are authoring only.** `audience Plus` under a `persona` tells the board, Studio
  and docs which audiences the persona is in. It changes nothing at runtime and is not in the executable model.
- **Cross-slice warning (PLAY1139).** A screen action, form or reaction that `invokes` a command whose audiences
  can be off while the invoker's are on can be refused as not available. Align the audiences, or accept and say
  why. `when` conditions are opaque and skipped, never guessed.

## Three things called audience

| Name | Is |
| --- | --- |
| Screenplay `audience` | Who a part of the application is available to; membership granted at runtime |
| Chronicle routing audience | An event-routing label in event options; never an event field of the model |
| Token `aud` claim | Who a token is for; read as `$context.identity.claims.aud` |

They are unrelated. Never use `aud` to decide availability by name-matching an audience.
A gate needing both an audience and a Box Office entitlement reads a host value
(`provide entitlements from host`) in `available when`.

## Provided context values

`provide <name> <Type>[ optional] from <source>` adds `$context.<name>` for everything beneath the level it
is written at: application, module, feature (features nest) or slice.

```screenplay excerpt
module Fleet
  provide region String from "Nordics"

  feature DataQuality
    slice StateView Vessels
      provide vessel Vessel optional from query VesselByKey by $context.arguments.vesselId
```

- **Sources:** `query <Query> [by <expression>, ...]` (a declared, keyed, snapshot, non-observable query whose
  result type equals the provided type), `host`, a literal, an expression over other `$context` values,
  or a `file`/inline code body under the line (not both with `from`).
- **Levels:** the nearest declaration of a name wins and must keep the type (PLAY1125). The same name twice at one level
  is PLAY1123. Resolved once per request, lazily.
- **Reserved names:** `tenant`, `identity`, `causedBy`, `causation`, `occurred`, `audiences`, `command`, `arguments`
  cannot be provided (PLAY1124).
- **State the key per job at the slice.** A slice states `$context.command.<key>` for a command and
  `$context.arguments.<key>` for a query. A feature-level provider cannot do both; override at the slice.
- **Required versus optional.** Without `optional` the value is required: when absent, the request is refused with a
  clear reason. With `optional`, code sees `null` and a gate reading it denies.
- **Absence versus failure.** Absence (no key, no row, host did not supply) is unknown, not an error. Failure (store down,
  body throws, timeout) fails the request as a retryable error, never a denial.
- Code reads `context.Values.<Name>` or `ValuesAs<T>()` on `CommandContext`, `QueryContext`, `RuleContext`,
  `PolicyContext` and `ProvideContext`.
- `host`, inline code and `file` sources are opaque: the reference evaluator reports a gate that reads one as
  unsupported, never allowed or denied. Executing `provide` reports PLAY0268 until the toolchain admits it; model
  it, review it, do not promise it runs.
- **Identity stays out of rules by default.** `RuleContext` sees `CausedBy`, not `Identity`; roles and claims belong in a
  `policy`. A `provide` derived from `$context.identity` makes that value readable in a rule, so write it
  only deliberately and say why.

## A complete model

Read [complete example](references/complete-example.md) for a model that compiles with gates, an availability, a persona audience and a provided value.

## Who owns what

Read [runtime ownership](references/runtime-ownership.md) when a request asks where a gate is enforced, how
audiences are granted, or whether application code can check one. Applications never call these directly.

## Review checks

Read [review checks](references/review-checks.md) before accepting a model that uses `available` or `provide`.

## Diagnostics

PLAY1119-1129 are `provide` (line shape, source, body, duplicates, reserved names, type override, query source, cycle).
PLAY1130-1141 are `audience`, `availability` and `available` (shape, duplicates, undeclared, cycle, unprovided value,
cross-slice invocation, gate tokens and parentheses). The full table is in the toolchain
[diagnostics](../cratis-screenplay-toolchain/references/diagnostics.md).
Fix by changing the model: declare the audience, name the availability, provide the value or correct the key; never remove a
gate to silence a warning.
