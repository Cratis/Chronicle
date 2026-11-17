---
name: cratis-screenplay-command-surface
description: Use when writing Screenplay command, event, constraint, authorization or strongly-typed value syntax. Not for deciding stream boundaries or declaring queries, screens, projections and reactive edges.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-screenplay-command-surface/SKILL.md -->

# The Screenplay write surface

Read [preferred syntax](../cratis-screenplay-toolchain/references/preferred-syntax.md) when writing or reviewing `.play`.

Everything that changes the system: the `command` (intent), the `event` it appends,
the rules that can refuse it, and the strongly-typed values they are built from.

## Locate the model

The folder of the project's existing `.play` files is the model; code-only `Source/` or `src/` does not qualify. Otherwise use repository-root `Screenplay/`
([discovery/version gates](../cratis-screenplay-event-modeling/references/file-layout.md#locate-the-model)).
Never put a model under `.cratis/` or hand-copy it.

## Product sources

Versions: toolchain `SKILL.md`. Binding lives in `Semantics/SemanticModelBinder*.cs` and
`Diagnostics/DiagnosticCodes.cs`; syntax docs are `identity.md`, `context.md`, `concepts.md`,
`purposes.md`, `events.md` and `diagnostics.md`. "Parses" is not "runs": see the
`cratis-screenplay-model-authoring` language reference.

## `concept` and `type`

```screenplay
concept InvoiceId : Uuid
concept DiscountPercentage : Decimal
  validate
    >= 0    message "A discount cannot be negative"
    <= 100  message "A discount cannot exceed 100 percent"
```

Primitives: `Uuid`, `String`, `Int`, `Decimal`, `Bool`, `Date`, `DateTime`; `Enum` is a separate kind. Use bare `pii` and `secret` on concepts (**compliance is inherited**) and
`type <Name>` for a composite shape. **Identifiers cannot be personal data or secrets** (PLAY0515).
⚠️ A value literally named `validate` in an enum is read as an empty validate block; write `@validate`.
Read [concepts and types](references/concepts-and-types.md) for pii/secret, enum numbers and the
links to [compliance](references/compliance.md) and [enum numbers](references/enum-numbers.md).

## The command block

Excerpt: the concepts, `InvoiceLine`, policy and shared event are declared elsewhere.
For a command-owned event, use the complete inline example in [inline events](references/inline-events.md).

```screenplay excerpt
command RegisterInvoice
  description "Registers a new invoice with its lines and payment terms"
  invoiceId      InvoiceId identifier
  invoiceNumber  InvoiceNumber
  lines          InvoiceLine[]
  note           String optional
  authorize CanManageInvoice
  validate
    invoiceNumber not empty                 message "Invoice number is required"
    invoiceNumber matches "^INV-[0-9]{6}$"  severity warning message "Must look like INV-000000"
  produces InvoiceRegistered
    for invoiceId
    registeredAt = $context.occurred
```

Type modifiers: `<Type>[]` for a collection, `<Type> optional` for absence.
`Type[] optional` makes the whole collection optional, not its items. Legacy
`Type?` still parses with information `PLAY0479`; use its repair or editor quick
fix to migrate. `--warnaserror` does not reject information diagnostics.
`reads X optional` is not supported yet.

⚠️ **The parser enforces no clause order.** The house order is description,
properties, `reads`, `authorize`, `validate`, `produces`/`handler`, `concurrency`.
Keep to it.

A file/code `handler` beside unconditional `produces`, with a resolved nonempty body,
binds as the v23 result contract: `produces` fixes the
events and the body owns their property values. Without `produces` or a `may produce` block
(ESM v35: the body returns an ordered subset of the declared events), with `produces ... when`,
or with an unresolved body (even with `may produce`) it fails binding with PLAY0800. A pending
`handler implementation` beside unconditional `produces` is a described command (ESM v23).
Prefer plain `produces` for declarative logic. Read [described handlers](references/described-handlers.md)
for the result contract, version gates, PLAY0695–0698 and Stage's STAGE-IMPL-002 refusal.

## `identifier` — the stream boundary decision

At most one command property may carry `identifier`. It names the value the
runtime uses as the event source id, so **choosing it is choosing the stream
boundary** — the highest-consequence decision in the slice. Leave it out only
when the runtime should allocate a fresh `Uuid` (a command that creates
something whose identity the caller does not supply); for a create command prefer
`generated identifier` below, which also keeps the id out of the rendered form.

**A command that creates something has no caller-supplied id.** Declare
`<id> <IdConcept> generated identifier` (ESM v7) and `returns <id>` when the
caller needs it back; plain `identifier` on a create command makes the rendered form ask the user to type
the id. Keep plain `identifier` for commands that act on an existing stream. Specifications pin the
allocated value with `for "<value>"`. Read
[generated values](references/generated-values-and-responses.md) before writing one;
the shared model is [shop generation](references/shop-generation.md).

The complete create-command example with its specification is in that reference.

- A second `identifier` on the same command is an error: *only one property can be
  the identifier*.
- `identifier` on an **event** property is an error: *an event never carries its
  event source id*. It travels in the event context.

## `reads` — state the command decides against

Excerpt: the `Account` read model, its projection and its keyed query are
declared elsewhere.

```screenplay excerpt
command TransferFunds
  sourceId      AccountId
  destinationId AccountId
  amount        Decimal
  reads Account as source by sourceId
  reads Account as destination by destinationId
  validate
    require source.balance >= amount
      message "The source account does not cover the transfer"
```

`by <property>` names the command property the read model is looked up by.
`as <alias>` names one read: a command that reads a view more than once needs an
alias on **every** read (`PLAY0410`), aliases are unique (`PLAY0411`) and must not
match a command property (`PLAY0412`). A `require` path uses the alias, or the
view name when that view is read once.

Opt eligible state reads into application-root `command reads protected` (v25); legacy reads stay
unprotected and reads/concurrency cannot combine (PLAY0783). Read [protected reads](references/protected-reads.md)
for admission, paths and PLAY0780–0786.
Rendering needs a recent Stage (the protected-reads reference has the CLI versions). Uniqueness still uses a constraint; reference success is not target realization.

Do not use `reads` to fetch data the caller could supply. A composite-key view's `by` block in a
command read is `PLAY0271`, or `PLAY0781` reason `by-parts` under `command reads protected`
(read-surface `references/named-keys.md`).

## `validate`

Each rule takes an optional `severity information|warning|error` and then an
optional `message "<text>"` (or `$strings.<key>`). The default severity is `error`.

The rule vocabulary (`not empty`, `min`/`max`, comparisons, `length`, literal text rules, `matches`, `all`, `rule <Name>`) is in
[references/validate-rules.md](references/validate-rules.md): read it when choosing or writing a rule form.
Read [text and match rules](references/text-and-match-rules.md) for ordinal `contains`,
`not contains`, `starts with`, `ends with` (v24), the portable linear match dialect,
PLAY0699/0787, Studio repairs and public pattern checks. Text rules are not conditions.

⚠️ **Validation severity is not compiler severity.** Every failed rule and
`require` rejects the command, at `information` and `warning` too; severity only
tells the UI how to present the failure.

**Whole-command rules** use `require <condition>` with an indented `message` and
optional `severity`, sharing the condition grammar with `produces … when`:
`and` binds tighter than `or`, parentheses group. A conditional rule is an
implication: `require isExtension == false or newEndDate > endDate`.

Date/DateTime comparisons need ESM v26 and Uuid `==`/`!=` needs
ESM v28; older binders report PLAY0268 (`screenplay --version`). Read
[date comparisons](references/date-comparisons.md) for ordering a retention window with
`require to >= from`, offsets and the parsed-but-not-runnable calendar and clock forms
(PLAY0812 to PLAY0814; do not author them into executable models), and
[Uuid equality](references/uuid-equality.md) for the same-concept rule and refused shapes.

**Rules whose logic is code.** A bare `rule <Name>` has no portable meaning (`PLAY0268`); a bodied rule binds only as
opaque code. Read [references/validate-rules.md](references/validate-rules.md) when a rule's logic must live in a `file`
or fenced code body (the Cratis render target refuses `java` bodies).

**Put format rules on the `concept`, not the command** (except date comparisons,
which bind only on command properties, not as concept rules). A concept carries its own
`validate` block and every use inherits it — that is Screenplay's type system, and
a rule that travels is worth more than one that is repeated.

## `authorize`

Excerpt: the policies are declared at the top of the model.

```screenplay excerpt
authorize IsAccountant
          or IsCustomerSelf

authorize (IsAccountant or IsFinance) and OwnsInvoice
```

Policy names are PascalCase. `and` binds tighter than `or`; parentheses group.
A continuation line extends the clause.

⚠️ Two adjacent policies synthesize an implicit `and` (`authorize A B` means `A and B`); write the operator.

- Several `authorize` lines on one command or query combine with AND, in authored
  order; a `module` or `feature` gate is ANDed too, so an `or` never bypasses another gate.
- Evaluation short-circuits left to right, module → feature → command. A policy implemented
  in code makes the reference runner report the outcome unsupported, never allow or deny.

## Inline events and destinations

Prefer inline `produces event <Name>` for an event owned by one command: `for`
defaults to its identifier; never copy that identifier into payload. For events
shared by commands or reactions, declare `event` separately and use plain
`produces <Name>` with explicit `for <identifier>` on commands. Reactions cannot
declare inline events. Rendered Arc commands ignore `for` (Stage#187).
Read [references/inline-events.md](references/inline-events.md) when declaring an event inline in `produces event`,
choosing or omitting `for` on a production, or fixing `PLAY0469`-`PLAY0478`.

## `produces`

Mapping sources that bind: a command property, a literal, `$context.occurred` and `$identity.*`; `for` must name the command `identifier` (`PLAY0273`); several unconditional `produces` are co-production. Read [references/constraints-produces-concurrency.md](references/constraints-produces-concurrency.md) when writing mappings, `tag`, or `produces when`.

## Routes and external operations

Commands never produce public events; external operations stay unadmitted (`PLAY0268`); systems are admitted once any declares an ability (ESM v50). Read [references/routes-and-external-operations.md](references/routes-and-external-operations.md) when choosing routes, `stream` overrides, public or foreign events, or `system`/`uses`/`operation` declarations (see also [operation intent](references/operations.md)).

## `concurrency`

One `concurrency` block per command; it is not bound (`PLAY0271`) and does not protect a `reads` decision. Read [references/constraints-produces-concurrency.md](references/constraints-produces-concurrency.md) for the dimensions.

## Generated values and responses (ESM v7 and v15)

UUID generation and responses bind as v7; concept `generate` strategies select v15, rules v20 and pattern/checksum rules v21; older
compilers refuse PLAY0268 (`screenplay --version`). Secrets and sequences remain refused;
keep uniqueness and freshness enforcement. Read
[references/generated-values-and-responses.md](references/generated-values-and-responses.md)
for `generated`, `generate`, `returns`, strict fixtures, freshness and renderer limits.

Further gates: stored stream `All` refusal, compliance (ESM v13), enum numbers (ESM v12).
Example: `cratis-screenplay-toolchain/references/generated-responses-example.md`; routes: `cratis-screenplay-toolchain/references/sources-and-streams.md`.

## `constraint` — uniqueness at append time

Chronicle's constraints enforce **uniqueness only**; use one when two concurrent appends must not both win. Do not use `constraint … file <Path>` for other rules (`PLAY0396`). Read [references/constraints-produces-concurrency.md](references/constraints-produces-concurrency.md) when writing a `unique` constraint, `released by`, or `unique event`.

## `policy` and `persona`

A policy has **exactly one** `require` line (`PLAY0441`) **or** one implementation, never both (`PLAY0440`). Read [references/policy-persona-seed.md](references/policy-persona-seed.md) when
declaring a policy (including `require anonymous` for a deliberately public command or query), a persona, or a `seed` block.
A policy that decides from recorded state declares `reads <Model> [as alias] [by property]` and a state condition in `require`
(`plan.dataQuality == true`, `article.author == $identity.id`); an absent read denies. See the same reference, "Policies that decide from recorded state".

A `user` line names a development session identity and the personas it plays; see the same reference, "Users".

`authorize` says who may act; whether the command exists for the caller at all is `available`, with `provide` for `$context` values: `cratis-screenplay-availability`.

## Event data subject

A trailing `subject` on an event property is report-only (`PLAY0270`); see [compliance](references/compliance.md#event-data-subject).

## `$context` and `$identity`

Four contexts, and **what each omits is load-bearing** — when writing a code body or a context/expression mapping, read
[context.md](references/context.md) for the full member lists, the declarative
`$context.` paths, and `$causedBy` / `$env` / `$strings`.

| Context | For | Deliberately omits |
| --- | --- | --- |
| Command | a command handler | — |
| Query | a query performer | — |
| Rule | a validation rule | **`Identity`** — validation does not see roles or claims |
| Policy | an authorization policy | **`CausedBy`, `Causation`** |

## Verify

- [ ] Validate the complete model root with warnings as errors; name the tool,
      version and result (versions: toolchain `SKILL.md`).
- [ ] No unintended `PLAY0478` or `PLAY0479` information remains.
- [ ] At most one command property carries `identifier`, and no event property does.
- [ ] Format rules live on the `concept` except command-only date comparisons;
      state-dependent rules are specifications. DateTime comparisons preserve UTC
      ticks; specs include missing-offset Validation rejection.
- [ ] Every `authorize` combining policies writes `and`/`or` explicitly.
- [ ] Protected reads opt in and meet admission; legacy reads and `concurrency` imply no protection.
- [ ] Constraints are `unique` forms; no `file` constraint stands in for another rule.
- [ ] Each policy has one `require` or one implementation, not both.
- [ ] Personal data is `pii` on the concept; operational secrets are `secret`;
      no identifier or subject concept carries either; run `--check purposes` separately.
- [ ] Described handlers distinguish binding from Unsupported body execution and Stage refusal.
- [ ] No event carries an optional property covering two situations.

Compiler versions and admission: `cratis-screenplay-toolchain/SKILL.md`.
Method: `cratis-screenplay-modeling-lifecycle` and `cratis-screenplay-slice-design`.

## Route near misses

- Deciding *which* commands and events exist: `cratis-screenplay-event-modeling`.
- Building read models from these events: `cratis-screenplay-projections`.
- Pinning the rejections and denials: `cratis-screenplay-specifications`.
- The parsed/bound/executed boundary: `cratis-screenplay-model-authoring`.
