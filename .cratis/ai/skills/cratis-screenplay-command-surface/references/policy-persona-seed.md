<!-- cratis-ai-managed: skills/cratis-screenplay-command-surface/references/policy-persona-seed.md -->
# Policies, personas and seed

## Contents

- `policy` and `persona`
- Users
- Anonymous policy
- Policy negation
- Policies that decide from recorded state
- `seed`

## `policy` and `persona`

```screenplay
policy IsAuthenticated
  require authenticated
policy IsAccountant
  require role "Accountant"
policy CanManageInvoice
  require role "InvoiceManager"
    or role "Accountant"
policy OwnsInvoice
  require claim "sub" matches subject
policy IsAdultCustomer
  file Policies/IsAdultCustomer.cs

persona Accountant
  description "Handles invoicing and collections"
  policy IsAccountant
```

Condition atoms: `authenticated`, `anonymous`, `role "<name>"`, and
`claim "<name>" matches subject | "<value>" | <path>`. A policy has **exactly one**
`require` line — continue the condition on deeper-indented lines instead of adding
a second (`PLAY0441`) — **or** one implementation (a tagged ` ```csharp ` block or
`file`), never both (`PLAY0440`). Declarative policies run in the reference
runner; a code policy binds as opaque ESM v3 and needs a target to evaluate it.
A `persona` is authoring metadata: the binder reports it as information `PLAY0270`
(report-only) and it does not block binding; an unknown policy it lists is an error.
Personas an `onboarding` block invites or grants must be declared here; see `cratis-screenplay-authentication-and-onboarding`.

## Users

Requires Screenplay 5.11.0 or later (`screenplay --version`). A `user` is a person a development or test session can act as: one top-level line naming the personas it plays. Personas are roles (`Accountant`); users are people (`kari`).

```screenplay
policy IsAccountant
  require role "Accountant"
policy IsInvoiceManager
  require role "InvoiceManager"

persona Accountant
  policy IsAccountant
persona InvoiceManager
  policy IsInvoiceManager

user kari "Kari Nordmann" as Accountant, InvoiceManager
user guest "Guest"
```

The name is the identity a session presents; the quoted display name is optional. A user plays only declared personas (`PLAY0888`), names are unique (`PLAY0889`), and a user with no persona is authenticated and holds no roles, which tests a signed-in person without access. A user holds the roles and claims its personas' policies give it.

Like `persona`, a user is syntax-only: it never enters the executable model, so the bytes and revision are unchanged and a model without users compiles as before. Users exist for tooling that exercises a running application; Stage offers them to Lens only when its development mode and its explicit users switch are both on, never in a published or rendered application.

## Anonymous policy

Requires Screenplay 4.231.0 (ESM v31; `screenplay --version`); older compilers reject
`require anonymous`. A policy with `require anonymous` exposes a public command or
query, such as sign-up. The condition always holds, for callers with and without an
identity alike. Only a model that declares one selects v31.

```screenplay
concept SubscriberId : Uuid
concept EmailAddress : String
policy PublicAccess
  require anonymous
module Newsletter
  feature Signup
    slice StateChange SubscribeToNewsletter
      command SubscribeToNewsletter
        subscriberId SubscriberId identifier
        email EmailAddress
        authorize PublicAccess
        produces event SubscriberSignedUp
          email EmailAddress = email
      screen SubscribeToNewsletter
        action SubscribeToNewsletter for screen
      specification SubscribingWithoutAnIdentity
        when SubscribeToNewsletter
          subscriberId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
          email = "ada@example.com"
        then SubscriberSignedUp
          email = "ada@example.com"
```

`anonymous` must be the command's or query's whole effective authorization:

| Code | Refused |
| --- | --- |
| PLAY0804 | combined with another condition in `require` |
| PLAY0805 | negated; write `authenticated` instead |
| PLAY0806 | combined with another policy in `authorize` |
| PLAY0807 | under or above another module, feature, command or query gate |

A specification of such a command or query may omit `given caller`; every other
authorized specification still needs one (`PLAY0389`).

**Security:** `anonymous` declares a public surface on purpose. Never use it to make a
model compile or to silence a missing-`authorize` or caller finding; model the real
policy instead, because a generated application renders it as an open endpoint.

## Policy negation

`not <condition>` is a portable ESM v7 policy-condition
form, not a named-policy operator in `authorize`. It can negate
atoms/groups, not an opaque inline/file implementation. Precedence is `not`,
then `and`, then `or`; parentheses override grouping. Double negation restores
a known value, not an unknown target.

Missing caller claims compared to a known text target have no match (false),
so their negation is true. A missing/null/non-text comparison target or absent
subject is instead **unknown**. `not unknown` stays unknown; final unknown denies.
Kleene combinations preserve known decisive values: true OR unknown is true,
false AND unknown is false; false OR unknown and true AND unknown stay unknown.
`not role "Service"` alone does not require authentication: retain an explicit
`authenticated` condition when a signed-in caller is required.

Opaque predicates reached in authored short-circuit order return unsupported,
never false or permission under negation. Binding warns with PLAY0546 when a
negated claim's target is optional, non-string, or a subject without a command
identifier. This C# semantic check does not come from ordinary editor validation.
Source: `Documentation/screenplay/policies.md`.

## Policies that decide from recorded state

A policy can decide from state the application has recorded (the plan of the asset a command
acts on, the author of an article) instead of only from the caller. It declares the read models
it consults with `reads` and compares what they hold in `require`.

Excerpt: the read models `AssetPlan` and `Article`, their projections and the commands are declared elsewhere.

```screenplay excerpt
policy DataQualityEnabled
  reads AssetPlan as plan by assetId
  require authenticated and plan.dataQuality == true

policy IsAuthor
  reads Article as article by slug
  require authenticated and article.author == $identity.id
```

- `reads <Model> [as <alias>] [by <property>]`. `by` names a property of the command or query being
  authorized (resolved like `claim "x" matches invoice.department`); it is the key of the read.
  Without `as`, the model name is the alias.
- A state condition is the shared condition operand (`<path> <operator> <value>`) used by
  `produces when` and `validate require`. The path starts at a read alias, a property of the artifact
  being authorized or `$identity`. It combines with `authenticated`, `role`, `claim`, `not`, `and` and `or`.
- A policy has `reads` **and** one `require`; it never also has an implementation (`PLAY0440`), and one `require` line
  (`PLAY0441`).
- The caller's identifier is `$identity.id`: the property must be required text and the operator `==` or `!=`;
  other caller details and artifact operands are refused. A specification names the caller's identifier with
  `id "alice"` under `given caller`.
- `contains` (a required text collection holds an element, or a required text property holds a substring) and
  `starts with` compare against a text constant, ordinal and case-sensitive. An absent value never matches.
- A policy can declare typed inputs that a command or query maps (`asset AssetId`, then under
  `authorize PlanGrants` a line `asset = vesselId`), so it does not depend on how an artifact spells its key.
  An unmapped input is filled from the artifact property of the same name. The mapping block belongs to one
  `authorize <Policy>` line.
- `narrow <Policy>` on a collection query drops, per row, the rows the policy denies (a row without recorded state is
  dropped too); `authorize` still refuses the whole call and composes with it. Not an `authorize` form.

**Fail-closed.** A read that finds nothing denies; a policy never allows because state is missing. In the generated
Arc policy, a missing key, a missing instance, a failing read and an unauthenticated caller (when `$identity.id` is
used and the principal carries none) all make the policy *unavailable*, and unavailable denies. A negated condition does
not turn absence into allow: `!=` against `$identity.id` still denies a caller that cannot say who it is. A specification
states the denial with a `given` read model (or none) and a `then` rejection of kind `Unauthorized`.

**Admission and rendering.** Screenplay admits these policies to the executable semantic model, so
they bind and the reference evaluator runs them from `given` read-model state. ESM versions: `reads` and state
conditions v44, `contains`/`starts with` v45, policy inputs v46, `narrow` v47, `$identity.id` v49. A model that uses none
of them is unchanged. Rendering is a separate step: Stage renders v44 to v47 policies as fail-closed
Arc authorization policies, and `$identity.id` reads the caller from the principal Arc authorized (Stage
`reference/authentication-rendering.md`). An unsupported shape is refused for that artifact with the construct named;
never weaken or drop the policy to render. The executable model admits a read property compared with a constant of its
scalar type (text, enumeration, number, boolean). Check the compiler version (`cratis-screenplay-toolchain`) before
relying on a version-specific form. Not verified end to end in this corpus: a generated application built in CI.

The hand-written counterpart in Arc is `StateAwareAuthorizationPolicy<TState>` (`cratis-arc-authentication-authorization-and-identity`).
Admission only: like every policy, the check runs when the call is admitted and revokes nothing later. A rule that must
hold under concurrent writers belongs in a protected command read (`protected-reads.md`).

Sources: Screenplay `Documentation/screenplay/policies.md`, decisions 0151, 0152, 0153, 0154, 0157.

## `seed`

Excerpt: `CustomerRegistered` is declared in a slice.

```screenplay excerpt
seed
  for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
    CustomerRegistered
      name = "Acme Corp"
```

Events append to that event source in declaration order, using the same mapping
expression grammar as `produces`. Several `seed` blocks accumulate. Seeding is
operational metadata, not part of executable behavior (`PLAY0270`).

