---
name: cratis-screenplay-read-surface
description: Declare the read side of a Cratis Screenplay `.play` model — the `readmodel` shape, `query` with `by`/`filter`/`scoped to`/`observable`/`performer`, and `screen` at its three levels of detail, including the data/action contract that later component bindings, templates, forms, toolbars, routes and outlets consume. Use when adding or changing a query, a read-model shape, or a screen in a `.play` model, or when deciding what a caller may narrow a result by. Do not use for how events fill the read model, and do not use for layouts, templates or forms.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-screenplay-read-surface/SKILL.md -->

# The Screenplay read surface

Read [preferred syntax](../cratis-screenplay-toolchain/references/preferred-syntax.md) when writing or reviewing `.play`.

What the system can be asked, and what a user sees. The `readmodel` declares the
shape, the `query` is the entry point, and the `screen` renders it.

## Locate the model

Look first for the project's existing `.play` files: the folder holding them is the
model. A new model goes under the repository's `Source/` or `src/` folder, else in a
`Screenplay/` folder at the repository root; never under `.cratis/`, which holds
configuration and the shared AI corpus only. This is the
conventional home for consumer-owned `.play` source; do not invent another
location or search the whole repository before checking it.

`cratis ai install` manages `.cratis/ai/`, never model files. Never
hand-copy Screenplay source between repositories. Keep Markdown that explains,
questions or navigates the model in the repository's documentation; the `.play`
source is the single flow model.

## Verified product sources

| Package | Version | Purpose |
| --- | --- | --- |
| `Cratis.Screenplay` | `4.31.0` | Original parser, validator and binder evidence |
| `Cratis.Screenplay` | main `fd18129` | Canonical optionality; changed examples compiled |
| `Cratis.Screenplay` | `4.66.0` (`c89198b`) | Which query shapes bind and render (`Semantics/`, `Documentation/screenplay/queries.md`) |

The update follows `types.md`, `queries.md` and `vscode.md` at that main commit
(after v4.52.0). Compilation does not establish query execution. Current equality-filter evidence:
Screenplay v4.145.0 (admitted in 4.143.0, decision 0069); read
[the verified sources and compiled example](references/equality-filters.md).

Checked against the Screenplay repository at tag `v4.31.0` (commit `355dffb`):
`Documentation/screenplay/{queries,readmodels,screens,policies,specifications,interactions}.md`
and decision 0010 established the original baseline. Changed examples use the
newer main commit above; do not attribute their verification to the old tag.

## `readmodel` — shape only

Excerpt, inside a `StateView` slice:

```screenplay excerpt
readmodel AccountBalance
  description "What the account is worth right now"
  balance   Decimal
  movements Int
  note      String optional
```

A read model declares what it **is** — nothing about what builds it. Whatever
builds it points at it with `=>`, and **exactly one thing may**: two builders is
error `PLAY0191`. See `cratis-screenplay-projections` for the builder side.

Use trailing `key` on top-level read-model properties, never `identifier`.
Explicit marks replace inference; otherwise a single-instance query's `by` supplies
identity (all such queries must agree). Only with no single-instance identifier
and an admitted list query does the binder infer an unambiguous `*Id`; queryless
read models need an explicit `key`. See named keys below for collection-key exclusions.
One explicit key binds byte-identically to equivalent inference. Several marks
form a composite key, supplied by name in query/read
`by` blocks. At Screenplay 4.135.0 composite keys and block queries remain
**authoring-only** (PLAY0268, #599); the scalar optional snapshot shape binds.
Read [named keys](references/named-keys.md) for complete examples, trigger/command
reads, all-parts fixtures, PLAY0660–0662 and command-read-only PLAY0661 repair.
A `=>` target resolving to an event is an event-target builder, not a read model;
keep event and read-model names distinct (see projections' public-event targets).

## `query`

Excerpt: the concepts, the read model and the policy are declared elsewhere.

```screenplay excerpt
query ListInvoices => InvoiceListReadModel[]
  description "Every invoice the caller may see, narrowed by status and customer"
  filter status     InvoiceStatus optional
  filter customerId CustomerId optional
  filter tenantId   TenantId from $context.tenant
  authorize IsAuthenticated
```

**Name the compiler and renderer.** Optional keyed queries and collection queries
bind, including `observable`. Since Screenplay 4.143.0, caller-supplied equality
filters on collections bind as ESM v14; `from`, single-result filters, `scoped to`,
`performer` and composite `by` remain PLAY0268, each with its reason. Read
[equality filters](references/equality-filters.md) for the admitted shape, a complete
compiled specification model and the Stage/CLI ceilings. Keep a list the domain needs.

Return-type forms after `=>`: `ReadModel`, `ReadModel optional`, `ReadModel[]`,
each optionally prefixed `observable`. `ReadModel[] optional` permits an absent
collection, not absent items. Legacy `Type?` reports information `PLAY0479`,
which does not fail `--warnaserror`; use a verified repair or editor quick fix.

Keep `query Q => observable?` only for a one-shot query returning an optional
scalar type named `observable`. It is the canonical exception and has no
`PLAY0479`: `observable optional` instead means a live result of a type named
`optional`. Do not migrate that exception.

### `by` vs `filter` — and why it is a security decision

| Clause | Meaning |
| --- | --- |
| `by` | the **identifying** parameter — the query returns the instance it identifies |
| `filter` | a caller-supplied equality parameter narrowing the result set; required unless typed `optional` |
| `from <source>` | fills the parameter **from the context** instead of from the caller |

⚠️ **Anything the caller must not be able to choose — the tenant, the caller's own
subject — belongs on a `from` parameter, never on a `filter` the UI supplies.**
Any mapping source works as a `from` source, so `$context.`, `$env.` and constants
are all available. The `from` states the contract; the runtime that realizes the
query enforces it. A compiling query is not evidence that access control works.

### `scoped to`

Excerpt, inside a `StateView` slice:

```screenplay excerpt
query Mine => Timesheet[]
  scoped to identity
query Everyones => Timesheet[]
  scoped to global
```

⚠️ **The tenant is the default and stays unstated.** A query is scoped to the
tenant it runs for unless it says otherwise, so `scoped to global` is how a query
*opts out* and reaches past the tenant. As the documentation puts it: *"Making the
narrow case the default means the dangerous case is the one you have to write
down, rather than the one you get by forgetting."*

**The scope is a name, not a closed set.** `identity` and `global` are the two the
language documents, but the grammar accepts any name — what scopes exist follows
the identity model of whatever runs the document. A query declares at most one.

At Screenplay 4.127.0, `scoped to` remains an opaque name, **not** a reference
to a declared identity detail. An `identity` query source must be keyed and return
one result (possibly optional), with a type compatible with the detail; its key
uses only caller built-ins/claims/literals and its effective gate cannot depend
on details (PLAY0641–0646). Source resolution is metadata, not runtime execution.
See command-surface `references/context.md` for the full contract.

Treat every `scoped to global` in a review as a question to answer, not a detail.

### `observable`

`=> observable OverdueInvoicesReadModel[]` declares a live read that keeps
pushing; without the marker a query is one-shot, which is the default and what
most reads are. The marker qualifies only *how* the result arrives, so it composes
with `[]`, `optional`, `by`, `filter`, `authorize` and a `performer`, and a screen binds
to a live query exactly as it binds to a one-shot one. The delivery itself is the
target runtime's: the executable model binds `observable` queries (delivery `live`) that
have no opaque scope/performer and, when filtered, only admitted caller-supplied equality filters (ESM v14).

Paging, sorting and change-set delivery for queries are accepted as Screenplay
decision 0010 but **not in the language yet**. Do not invent syntax for them.

### `performer`

A query is **complete without a performer** — it is realization metadata, not a
precondition. It takes a `file` reference or an inline block in a tagged fence
(` ```csharp `, ` ```sql `), and is the query's counterpart to a command's
`handler`. The parser accepts any registered inline language and does not reject
a nonsensical one, so the choice is yours to get right. Performer code is opaque:
Screenplay never runs it.

A read model a performer composes needs no projection, and no event builds it.
Specify it from state with `given readmodel`, then `when query <Query>` with the
arguments and `then result` (v4.48.0) - see `cratis-screenplay-specifications`.

A query's key goes on its own `by` line in the body. `query X => RM optional by id Type`
on the header line is a declaration error, not a shorthand.

### What runs

These bind on standalone 4.149.0: keyed `=> RM optional`, unfiltered collections,
collections narrowed by caller-supplied `by`, and their `observable` forms.
Caller-supplied equality filters on collections select ESM v14: same-named scalar
primitive/concept/enum properties, same nominal type ignoring optionality, no
`from`, distinct names not repeating `by`. `by` and supplied filters AND together;
optional filters may be omitted, but supplied null is refused. PLAY0268 still
rejects context sources, single-result filters, opaque scope/performer and
composite `by` (#599); a single non-optional result remains unadmitted too.
Authorization runs before lookup, ANDed with module/feature gates; policies see
only `by`, never filters. Pin denial with `given caller` and `then denied`.
Specs pass `by` and filters by name (PLAY0468 for unknown, repeated or missing
required arguments); collection results match by read model/key regardless of
row order. Details and renderer limits: [equality filters](references/equality-filters.md).

## `screen` — three levels

The examples below are excerpts: each screen sits in a slice, and the queries,
commands and screens it names are declared elsewhere.

**Level 1 — intent.** Data and actions; the tool generates the component.

```screenplay excerpt
screen InvoiceList
  data InvoiceListReadModel[] via query ListInvoices
  section registerInvoiceInput
    title "Register invoice"
    on click
      navigate to RegisterInvoiceScreen
  action CancelInvoice
```

**Level 2 — structure.** Named sections, tables and summaries filling a template's
slots.

```screenplay excerpt
screen InvoiceDetails
  template MasterDetail
    sidebar
      data InvoiceDetailsReadModel via query GetInvoice by invoiceId
      summary InvoiceDetailsReadModel
        field invoiceNumber label "Invoice #"
      section actions
        action CancelInvoice
    main
      section lineItems
        table lineItems
          column lineNumber label "#"
          column quantity   label "Qty"
          on row-click navigate to InvoiceLineDetail by lineNumber
```

**Level 3 — inline code.** The surrounding Screenplay context supplies the typed
data contract; the inline block receives it as `Props`. Languages: `react`,
`typescript`, `html`, `csharp`.

Constructs: `title`, `data … via query … [by <param>]`, `action <Command>` with
`label` and `navigate to <Screen> [by <param>]`, `section <name>`,
`table <target>` with `column <property> [label]` and
`on row-click navigate to <Screen> [by <param>]`, `summary <ReadModel>` with
`field <property> label`, and `template <Name>` with slot bodies.

The screen's `data` and `action` declarations are the typed context for later UI
composition. Component `context`, toolbar parameters, dialog inputs, outlet routes
and manual form columns must all trace back to this screen contract, a form's
command, a selected item, or `$context`. If a generated app shows a default list
or empty panel because it could not consume one of those directives, treat that as
a dropped UI construct, not as an acceptable Level 1 screen.

`on row-click navigate to …` is the one-line table navigation form. What a click,
selection, submit or screen entry *does* beyond that — confirm, execute, refresh,
open a dialog, branch on success or failure — is an interaction: an inline `on`
block or a named `behavior` attached with `uses`. See
`cratis-screenplay-ui-composition`. Screens and interactions are deferred from the
executable model (information `PLAY0269`); they never block binding.

## Guarded screen actions

One labeled user decision can choose among commands using ordered
`when item.<path> ... execute <Command>` alternatives, with optional last
`otherwise execute` or `otherwise hidden`. First match wins; no item or no match
without an execute fallback hides it. Inputs use typed `with ... from ...`
bindings; the selected command still enforces its own gate/rules/constraints.
Read [guarded actions](references/guarded-actions.md) for the checked model,
missing-versus-null behavior and source-versus-renderer boundary. For a gesture
choosing a whole action list rather than one labeled command, use block-form
`when`/`otherwise` on click/double click/select:
[guarded interactions](../cratis-screenplay-ui-composition/references/guarded-interactions.md).

## How a bare name resolves

**Inside out:** the slice, then the enclosing feature, then the module, then the
document. The innermost match wins.

That rule exists because a generated document cannot make every name unique — one
real application declares 76 queries under 37 distinct names, with `All` appearing
21 times. Two sibling slices can each declare `All`, and each screen gets its own.

Reach across slices by qualifying with **any trailing part** of the scope.
Excerpt: `Queue` and `Deviations` are sibling slices that each declare `All`.

```screenplay excerpt
screen OverviewScreen
  data QueueReadModel[]     via query Queue.All
  data DeviationReadModel[] via query Preparation.Deviations.All
```

Use the shortest unambiguous form. If a bare name matches two declarations equally
well, the compiler **warns and names the candidates** rather than picking one:

```text
Ambiguous query 'All' - it matches 2 declarations equally well
(Invoicing.Preparation.Queue, Invoicing.Preparation.Deviations); qualify it to say which
```

⚠️ Unresolved and ambiguous references are **warnings, not errors**, because a
name may resolve to something outside the document. Run with `--warnaserror` or a
screen can navigate to a screen that does not exist and the build stays green.
This is about references only: malformed clauses, duplicate declarations and
binding failures are errors.

## Verify

- [ ] Standalone `screenplay <model> --warnaserror` (4.68.0) reports zero errors and zero
      warnings; or `cratis screenplay validate --warnings-as-errors` on the model folder
      (3.28.2 bundles the same Screenplay 4.66.0; before 3.28.2 bundled 4.60.1, ESM v5
      or lower). Name which tool produced the result.
- [ ] Every value the caller must not choose is a `from` parameter, not a `filter`.
- [ ] Every `scoped to global` is deliberate and defensible.
- [ ] `observable` is present exactly where the caller should see changes without
      asking again.
- [ ] Every authorized query has a specification with `given caller`, including a
      `then denied` case.
- [ ] Each read model has exactly one builder and every field traces to an event.
- [ ] No screen reference is left ambiguous or unresolved.
- [ ] Every UI binding that consumes screen data (component context, toolbar
      parameter, dialog input, outlet route) has a source in the screen contract
      or an explicitly provided template/form context.

Read `references/versions.md` in `cratis-screenplay-toolchain` when choosing a tool, checking versions, or assessing executable and renderable subsets. Where a construct sits in the
method: `cratis-screenplay-modeling-lifecycle` and `cratis-screenplay-slice-design`.

Read [screen patterns](references/screen-patterns.md) when deciding whether a screen is an input, status or error-state screen and which `data` and `action` lines it needs.

## Route near misses

Attribution and licences: `references/provenance.md`.

- How events fill the read model: `cratis-screenplay-projections`.
- Layouts, templates, forms, component bindings, toolbars, outlets, contributions, packages and themes: `cratis-screenplay-ui-composition`.
- Asserting query results: `cratis-screenplay-specifications`.
- Deciding which read models exist: `cratis-screenplay-event-modeling`.
