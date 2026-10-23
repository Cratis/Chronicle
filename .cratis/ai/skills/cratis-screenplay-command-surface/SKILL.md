---
name: cratis-screenplay-command-surface
description: Write the write side of a Cratis Screenplay `.play` model — the `command` block and its `identifier`, `reads`, `validate`, `authorize`, `produces`, `handler` and `concurrency` clauses, plus `event`, `constraint`, `policy`, `persona`, `concept`, `type` and `seed`. Use when declaring or changing a command, an event shape, a validation or authorization rule, an append-time constraint, or a strongly-typed value in Screenplay. Do not use for projections, queries, screens, captures or reactions.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-screenplay-command-surface/SKILL.md -->

# The Screenplay write surface

Read [preferred syntax](../cratis-screenplay-toolchain/references/preferred-syntax.md) when writing or reviewing `.play`.

Everything that changes the system: the `command` that expresses intent, the
`event` it appends, the rules that can refuse it, and the strongly-typed values
they are all built from.

## Locate the model

Look first for the project's existing `.play` files: their folder is the model.
For a new model, check `Source/` or `src/`, else use `Screenplay/` at the repository
root. Never put a model under `.cratis/` or hand-copy it between repositories.
`cratis ai install` manages `.cratis/ai/`, never model files. Keep explanatory
Markdown in project documentation; `.play` is the single flow model.

## Product sources

Tool versions: toolchain `SKILL.md`; sources below. Compilation is not execution.

- Examples and executable boundaries; inline events, repairs, canonical `optional`.
- `handler`, code attachments, `persona` and compliance binding (`Semantics/SemanticModelBinder*.cs`, `Diagnostics/DiagnosticCodes.cs`).
- Generated-value and response rules (ESM v7).
- Identity and compliance syntax (`identity.md`, `context.md`, `concepts.md`, `purposes.md`, `events.md`, `diagnostics.md`); v13 compliance admission: `references/compliance.md`.
- v15 generation strategies, strict fixtures and freshness; Stage realization: `references/generated-values-and-responses.md`; shop: `references/shop-generation.md`.

"Parses" and "runs" are different claims. The executable profile, and what
each construct binds to, is in the `cratis-screenplay-model-authoring` language
reference.

## `concept` and `type`

```screenplay
concept InvoiceId : Uuid
concept DiscountPercentage : Decimal
  validate
    >= 0    message "A discount cannot be negative"
    <= 100  message "A discount cannot exceed 100 percent"
concept PersonName : String pii
  pii reason "Identifies the billing contact."
concept InvoiceStatus : Enum
  draft
  sent
  paid
```

Primitives: `Uuid`, `String`, `Int`, `Decimal`, `Bool`, `Date`, `DateTime`.
`Enum` is a separate concept kind; details: `references/enum-numbers.md`.
Use bare `pii` (`personal` alias) and `secret` on concepts; **Compliance is inherited**.
Keep protection even when a compiler or renderer refuses it. Read
[compliance and purposes](references/compliance.md) for ESM v13, binder-only refusals,
consumer ceilings, marker repairs, reasons, scopes and provider limits.

**Identifiers cannot be personal data or secrets.** PLAY0515 is a source error
for command identifiers, typed `for` destinations, event-source identities,
scalar/composite stream-id types and nested route mapping sources, including legacy
markers. Keep a surrogate identity and carry protected data in the payload.
Chronicle likewise rejects `[PII]`/`[Encrypted]` identifiers (CHR0034/CHR0052).

Read [enum numbers](references/enum-numbers.md) for `draft = 1`, unique int32
all-or-none pins, stored/public identity safety, `enum-pinning`, ESM v12 and the
CLI/Stage gap. Unnumbered enums keep positions; specifications use names.

⚠️ **Enum trap.** A value literally named `validate` is read as an empty validate
block. Write `@validate` for the value; the compiler warns when it sees the
ambiguity.

Use `type <Name>` for a composite shape (several properties) that events and
commands reference; use `concept` for a single wrapped primitive.

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

A file/code `handler` cannot coexist with `produces` and fails binding
with PLAY0800 (`CommandHandlerRequiresAttachment`): gap-fill the slice in code
with a constrained implementation attachment, not by waiting for a new ESM version.
Read the required typed `subject` argument for the command, never parse the message.
Requires Screenplay 4.210.0 (`screenplay --version`) for PLAY0800; older binders use PLAY0268 for this refusal. A pending `handler implementation` beside unconditional `produces`
is a described command (ESM v23): the body owns payload values only. Prefer plain
`produces` for declarative logic. Read [described handlers](references/described-handlers.md)
when logic needs prose, for the result contract, PLAY0695–0698, Unsupported
ImplementationBody and Stage's STAGE-IMPL-002 refusal (no stubs, even in Stub mode).

## `identifier` — the stream boundary decision

At most one command property may carry `identifier`. It names the value the
runtime uses as the event source id, so **choosing it is choosing the stream
boundary** — the highest-consequence decision in the slice. Leave it out only
when the runtime should allocate a fresh `Uuid` (a command that creates
something whose identity the caller does not supply).

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

Opt eligible state reads into application-root `command reads protected` (v25).
Absence rejects; matching intervening facts conflict. Read [protected reads](references/protected-reads.md)
for strict admission, paths and PLAY0780–0786. Legacy reads stay unprotected;
reads/concurrency cannot combine (PLAY0783). Semantic Host/executor support is separate.
Requires Stage 4.138.0 to render protected reads as Arc read protection (shapes it cannot preserve stay STAGE-ESM-044); `cratis render` gets it from the first CLI that bundles it, and CLI 3.75.0 refuses every protected read with STAGE-ESM-044.
Uniqueness still uses a constraint; reference success is not target realization.

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

Requires Screenplay 4.199.0 (ESM v26; `screenplay --version`) for Date/DateTime
comparisons; older binders report PLAY0268. Read [date comparisons](references/date-comparisons.md)
when ordering a retention window with `require to >= from`, comparing instants
across offsets, or rejecting offset-less inputs before rules. It covers admitted
positions, PLAY0268/0273, ordinal `then` matching and Stage's STAGE-ESM-046 renderer refusal.

Requires Screenplay 4.205.0 (ESM v28; `screenplay --version`) for `==`/`!=` between Uuid
values (properties, concepts, identities, canonical literals, and read paths in `require`);
older binders report PLAY0268. Read [Uuid equality](references/uuid-equality.md) for the same-concept
rule, PLAY0268/0273 and the refused shapes (ordering, Uuid validation rules, Uuid concept rules).

**Rules whose logic is code.** A bare `rule <Name>` has no portable meaning (`PLAY0268`) and a bodied rule binds only as
opaque code. Read [references/validate-rules.md](references/validate-rules.md) when a rule's logic must live in a `file`
or fenced code body.

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

⚠️ **Two adjacent policies synthesize an implicit `and`.** `authorize A B` means
`A and B`. Write the operator explicitly so the reader does not have to know this.

- Several `authorize` lines on one command or query combine with AND, in authored
  order.
- `authorize` on an enclosing `module` or `feature` is ANDed with the command's
  own gate; an `or` inside one gate never bypasses another gate.
- Evaluation short-circuits left to right, module → feature → command. If it
  reaches a policy implemented in code, the reference runner reports the outcome
  unsupported; it never guesses allow or deny.

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

Commands never produce public events; external work stays unadmitted (`PLAY0268`). Read [references/routes-and-external-operations.md](references/routes-and-external-operations.md) when choosing routes, `stream` overrides, public or foreign events, or `system`/`uses`/`operation` declarations (see also [operation intent](references/operations.md)).

## `concurrency`

One `concurrency` block per command; it is not bound (`PLAY0271`) and does not protect a `reads` decision. Read [references/constraints-produces-concurrency.md](references/constraints-produces-concurrency.md) for the dimensions.

## Generated values and responses (ESM v7 and v15)

UUID generation/responses bind as v7; serialized concept `generate` selects v15:
UUIDv7/ULID/TypeID/NanoID/bytes, strict fixtures and whole-source freshness.
Reuse rejects Concurrency, no append/retry. Rules select v20 (Requires Screenplay
4.164.0, `screenplay --version`); pattern/checksum, Int random and attached rules
select v21 (Requires 4.172.0). Older compilers refuse PLAY0268. Secrets and sequences
remain refused. Patterns are readable references, not gapless allocators or secrets;
check digits catch typos, not forgery. Keep uniqueness/freshness enforcement.
Read [references/generated-values-and-responses.md](references/generated-values-and-responses.md)
for `generated`, `generate`, `returns`, strict fixtures/freshness and renderer limits.

Requires Screenplay 4.147.0 or later (`screenplay --version`) for concept generation strategies (ESM v15), 4.150.0 or later for the stored stream `All` refusal, 4.141.0 or later for compliance (ESM v13) and 4.139.0 or later for non-positional enum numbers (ESM v12).
Complete example: `cratis-screenplay-toolchain/references/generated-responses-example.md`.
Routes and realization: `cratis-screenplay-toolchain/references/sources-and-streams.md`.

## `constraint` — uniqueness at append time

Chronicle's constraints enforce **uniqueness only**; use one when two concurrent appends must not both win. Do not use `constraint … file <Path>` for other rules (`PLAY0396`). Read [references/constraints-produces-concurrency.md](references/constraints-produces-concurrency.md) when writing a `unique` constraint, `released by`, or `unique event`.

## `policy` and `persona`

A policy has **exactly one** `require` line (`PLAY0441`) **or** one implementation, never both (`PLAY0440`). Read [references/policy-persona-seed.md](references/policy-persona-seed.md) when
declaring a policy (including `require anonymous` for a deliberately public command or query), a persona, or a `seed` block.

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
      version and result. Tool versions: toolchain `SKILL.md`;
      bundle compatibility and execution are separate checks.
- [ ] No unintended `PLAY0478` or `PLAY0479` information remains.
- [ ] At most one command property carries `identifier`, and no event property does.
- [ ] Format rules live on the `concept` except command-only date comparisons;
      state-dependent rules are specifications. DateTime comparisons preserve UTC
      ticks; specs include missing-offset Validation rejection.
- [ ] Every `authorize` combining policies writes `and`/`or` explicitly.
- [ ] Protected reads opt in, meet admission and distinguish reference protection
      from target realization; legacy reads and `concurrency` imply no protection.
- [ ] Constraints are `unique` forms; no `file` constraint stands in for another rule.
- [ ] Each policy has one `require` or one implementation, not both.
- [ ] Personal data is `pii` on the concept; operational secrets are `secret`;
      no identifier or subject concept carries either. Reasons explain the value,
      while purposes declare basis/retention; run `--check purposes` separately.
- [ ] Described handlers distinguish binding from Unsupported body execution and
      Stage refusal; attached handlers remain unbound. Name the admitting tool.
- [ ] No event carries an optional property covering two situations.

Compiler versions and admission: `cratis-screenplay-toolchain/SKILL.md`.
Method: `cratis-screenplay-modeling-lifecycle` and `cratis-screenplay-slice-design`.

## Route near misses

- Deciding *which* commands and events exist: `cratis-screenplay-event-modeling`.
- Building read models from these events: `cratis-screenplay-projections`.
- Pinning the rejections and denials: `cratis-screenplay-specifications`.
- The parsed/bound/executed boundary: `cratis-screenplay-model-authoring`.
