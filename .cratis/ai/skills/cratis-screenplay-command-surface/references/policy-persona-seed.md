<!-- cratis-ai-managed: skills/cratis-screenplay-command-surface/references/policy-persona-seed.md -->
# Policies, personas and seed

## Contents

- `policy` and `persona`
- Anonymous policy
- Policy negation
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

