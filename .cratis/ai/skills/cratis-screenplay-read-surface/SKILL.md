---
name: cratis-screenplay-read-surface
description: Use when writing Screenplay read-model shapes, query filtering or screen data and action contracts. Not for event-to-state mappings or component layouts, templates and forms.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-screenplay-read-surface/SKILL.md -->

# The Screenplay read surface

Read [preferred syntax](../cratis-screenplay-toolchain/references/preferred-syntax.md) when writing or reviewing `.play`.

What the system can be asked and what a user sees: the `readmodel` is the shape,
the `query` the entry point, the `screen` the rendering.

## Locate the model

The folder holding the project's existing `.play` files is the model; code-only
`Source/` or `src/` does not qualify. Otherwise use repository-root `Screenplay/`,
never `.cratis/`. Read [discovery/version gates](../cratis-screenplay-event-modeling/references/file-layout.md#locate-the-model)
and do not invent another location.

`cratis ai install` manages `.cratis/ai/`, never model files. Never hand-copy
Screenplay source between repositories; the `.play` source is the single flow model.

## Product sources

Screenplay `Documentation/screenplay/{queries,readmodels,screens,policies,specifications,interactions,types,vscode}.md`
describe parser, validator and binder behavior, including which query shapes bind and render
(`Semantics/`). Compilation does not establish query execution. Equality filters: see
[sources and compiled example](references/equality-filters.md).

## `readmodel` — shape only

Excerpt, inside a `StateView` slice:

```screenplay excerpt
readmodel AccountBalance
  description "What the account is worth right now"
  balance   Decimal
  movements Int
  note      String optional
```

A read model declares what it **is**, not what builds it. The builder points at it
with `=>`, and **exactly one thing may** (two builders: `PLAY0191`). See `cratis-screenplay-projections` for the builder side.

Use trailing `key` on top-level read-model properties, never `identifier`.
Explicit marks replace inference; otherwise a single-instance query's `by` supplies
identity (all such queries must agree). Only with no single-instance identifier
and an admitted list query does the binder infer an unambiguous `*Id`; queryless
read models need an explicit `key`. One explicit key binds byte-identically to
equivalent inference. Several marks
form a flat composite key: an optional single-instance query names every part in a
`by` block, a list query narrows with equality filters, and fixtures state every part.
Requires Screenplay 4.204.0 (ESM v27; `screenplay --version`); older binders refuse
several key marks with PLAY0268. A list query over such a read model cannot use a
`by` block (PLAY0268).
Read [named keys](references/named-keys.md) for a complete example, collection-key exclusions,
trigger/command reads, all-parts fixtures, PLAY0660–0662 and PLAY0661 repair.
A `=>` target resolving to an event is an event-target builder, not a read model;
keep event and read-model names distinct (see projections' public-event targets).

## Where a read model is stored

No `sink` line means the default sink. `sink <Name>` stores it in a root-declared
`sink <Name> for readmodels`, never on a `passive` read model (`PLAY0935`); it is the directive only
when `<Name>` is a declared sink, else a property. `@sink <Type>` is always the property.
Read [sinks](references/sinks.md) before naming a sink.

## Passive read models

A bare `passive` line in a read model body marks one that only informs a command's decision.
It is never inferred, and a query or screen that consumes it is error `PLAY0859`. Read
[passive read models](references/passive.md) before declaring one.

## `query`

A query's HTTP shape and a screen's data contract are exported (`screenplay export --format openapi|ui`; [formats](../cratis-screenplay-toolchain/references/model-export-diff-rename.md#openapi-and-ui-contract-formats)).

A query that ends in `graph <Graph>` reads a graph: see [graph queries](references/graph-queries.md). Such a query may return nested rows with `collect` (a view-shaped query; see [collect nested rows](../cratis-screenplay-projections/references/graphs.md#collect-nested-rows)).

Excerpt: the concepts, the read model and the policy are declared elsewhere.

```screenplay excerpt
query ListInvoices => InvoiceListReadModel[]
  description "Every invoice the caller may see, narrowed by status and customer"
  filter status     InvoiceStatus optional
  filter customerId CustomerId optional
  filter tenantId   TenantId from $context.tenant
  authorize IsAuthenticated
```

A deliberately public query uses a policy with `require anonymous` as its whole
authorization, ideally in a separate public module; omitting `authorize` is not the same. Rules and diagnostics:
[anonymous policy](../cratis-screenplay-command-surface/references/policy-persona-seed.md#anonymous-policy).

**Name the compiler and renderer.** Optional keyed queries and collection queries
bind, including `observable`. Caller-supplied equality
filters on collections bind as ESM v14; `from`, single-result filters, `scoped to`,
and a `by` block on a list query (or on a non-optional single result) remain PLAY0268, each with its reason. A `performer` query is not refused: it is an opaque read (PLAY0270 information) left out of the executable model ([version gate](../cratis-screenplay-toolchain/references/executable-subset.md#code-attachments-what-binds-what-never-does)). Read
[equality filters](references/equality-filters.md) for the admitted shape, a complete
compiled specification model and the Stage/CLI ceilings.

Concept keys/supplied filters validate after authorization: invalid means Validation,
not absence; omitted optional filters are unchecked. Read [query rejections](../cratis-screenplay-specifications/references/query-rejections.md) when specifying this.
Requires Screenplay 4.161.0 (`screenplay --version`); older runners may answer absence.

Return-type forms after `=>`: `ReadModel`, `ReadModel optional`, `ReadModel[]`,
each optionally prefixed `observable`. `ReadModel[] optional` permits an absent
collection, not absent items. Legacy `Type?` reports information `PLAY0479`,
which does not fail `--warnaserror`; use a verified repair or editor quick fix.

Keep `query Q => observable?` only for a one-shot query returning an optional
scalar type named `observable`: the canonical exception, with no `PLAY0479`
(`observable optional` means a live result of a type named `optional`).

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
*opts out* and reaches past the tenant: the dangerous case is the one you write down.

**The scope is a name, not a closed set.** `identity` and `global` are documented,
but the grammar accepts any name; a query declares at most one.
`scoped to` is an opaque name, **not** a reference to a declared identity detail. An `identity`
query source must be keyed and return one result, with a compatible type, a key from caller
built-ins/claims/literals and a gate that cannot depend on details (PLAY0641–0646). See
command-surface `references/context.md` for the full contract.

Question every `scoped to global` in review.

### `observable`

`=> observable OverdueInvoicesReadModel[]` declares a live read that keeps
pushing; without the marker a query is one-shot, which is the default and what
most reads are. The marker qualifies only *how* the result arrives, so it composes
with `[]`, `optional`, `by`, `filter`, `authorize` and a `performer`, and a screen binds
to a live query exactly as it binds to a one-shot one. Delivery is the
target runtime's: the executable model binds `observable` queries (delivery `live`) that
have no opaque scope/performer and, when filtered, only admitted caller-supplied equality filters (ESM v14).

Paging, sorting and change-set delivery for queries are planned but **not in the language yet**. Do not invent syntax for them.

### `performer`

A query is **complete without a performer** — it is realization metadata, not a
precondition. It takes a `file` reference or an inline block in a tagged fence
(` ```csharp `, ` ```sql `), and is the query's counterpart to a command's
`handler`. The parser accepts any registered inline language (`csharp`, `typescript`, `react`,
`html`, `sql`, `java`) and rejects none. A performer query refuses a `by` or
`filter` argument sourced from `$context` with PLAY0810 (older compilers
silently drop it): take a caller-supplied argument instead. Performer code is opaque: Screenplay never
runs it. It is reported as information `PLAY0270` and left out of the executable model, with any
specification that reads it. Older compilers refuse it with PLAY0268: see the
[version gate](../cratis-screenplay-toolchain/references/executable-subset.md#code-attachments-what-binds-what-never-does).

A read model a performer composes needs no projection, and no event builds it.
Specify it from state with `given readmodel`, then `when query <Query>` with the
arguments and `then result` - see `cratis-screenplay-specifications`.

#### Performer bodies are rendered

Read [performer bodies](references/performer-bodies.md) when a query needs a body the declarative form cannot say (for example a filter relative to today), or when deciding rendering versus gap-fill.


A query's key goes on its own `by` line in the body. `query X => RM optional by id Type`
on the header line is a declaration error, not a shorthand.

### What runs

Keyed `=> RM optional`, unfiltered collections, collections narrowed by caller-supplied `by`
or equality filters (ESM v14) and their `observable` forms bind;
authorization runs before lookup and policies see only `by`. Pin denial with `given caller`
and `then denied`. Read [query execution](references/query-execution.md) for the filter rules,
PLAY0268, PLAY0468 and the reducer-view `PLAY0690` warning.

## `screen` — three levels

Excerpts: each screen sits in a slice, and the queries, commands and screens it names are
declared elsewhere. A screen declared outside a slice states `data <Shape>[]` with no
`via query` (a need) and a slice provides it: `cratis-screenplay-ui-composition`, `references/free-screens.md`.

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

**Level 2 — structure** fills a template's slots with named sections, tables and summaries;
**Level 3 — inline code** receives the typed data contract as `Props` (`react`, `typescript`,
`html`, `csharp`). A screen's `data` and `action` declarations are the typed context for UI
composition: if a generated app shows a default list or empty panel because it could not consume
one of those directives, treat that as a dropped UI construct, not an acceptable Level 1 screen.
Screens and interactions are deferred by default (information `PLAY0269`); `screenplay export --screens` admits the neutral parts as ESM v42 (data, actions, navigation, layout; never blocking binding); what a
click or submit *does* is an interaction (`cratis-screenplay-ui-composition`). Read
[screen levels](references/screen-levels.md) for the Level 2 example and the construct list.

## Guarded screen actions

One labeled user decision can choose among commands using ordered
`when item.<path> ... execute <Command>` alternatives, with optional last
`otherwise execute` or `otherwise hidden`. First match wins; no item or no match
without an execute fallback hides it. Inputs use typed `with ... from ...`
bindings; the selected command still enforces its own gate/rules/constraints.
Read [guarded actions](references/guarded-actions.md) for the checked model,
missing-versus-null behavior and source-versus-renderer boundary. For a gesture
choosing a whole action list rather than one labeled command, use block-form
`when`/`otherwise` on click, double click, select or change:
[guarded interactions](../cratis-screenplay-ui-composition/references/guarded-interactions.md).

`available` and `provide`: `cratis-screenplay-availability`.

## How a bare name resolves

**Inside out:** the slice, then the enclosing feature, then the module, then the
document. The innermost match wins.

Sibling slices may each declare `All`; each screen resolves its own query.
Read `references/named-keys.md` when query scope affects identity inference.

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

- [ ] Standalone `screenplay <model> --warnaserror` or `cratis screenplay validate --warnings-as-errors`
      reports zero errors and warnings (an older bundled compiler can stop at a lower ESM version). Name the tool.
- [ ] Every value the caller must not choose is a `from` parameter, not a `filter`.
- [ ] Every `scoped to global` is deliberate and defensible.
- [ ] `observable` is present exactly where the caller should see changes without
      asking again.
- [ ] Every authorized query has a specification with `given caller`, including a
      `then denied` case (a query authorized only by an anonymous policy may omit `given caller`).
- [ ] No query or screen consumes a `passive` read model (`PLAY0859`); a command reads each one (`PLAY0860`).
- [ ] Each read model has exactly one builder and every field traces to an event.
- [ ] A reducer-built view's `by` type matches its events' event-source identity (`PLAY0690`).
- [ ] No screen reference is left ambiguous or unresolved.
- [ ] Every UI binding that consumes screen data (component context, toolbar
      parameter, dialog input, slot route) has a source in the screen contract
      or an explicitly provided template/form context.

Read `SKILL.md` in `cratis-screenplay-toolchain` when choosing a tool, reading installed versions, or assessing executable and renderable subsets. Where a construct sits in the
method: `cratis-screenplay-modeling-lifecycle` and `cratis-screenplay-slice-design`.

Read [screen patterns](references/screen-patterns.md) when deciding whether a screen is an input, status or error-state screen and which `data` and `action` lines it needs.

## Route near misses

Attribution and licences: `references/provenance.md`.

- How events fill the read model: `cratis-screenplay-projections`.
- Layouts, templates, forms, component bindings, toolbars, hooks and placement, slots, outlets, contributions, sample data, packages and themes: `cratis-screenplay-ui-composition`.
- Asserting query results: `cratis-screenplay-specifications`.
- Deciding which read models exist: `cratis-screenplay-event-modeling`.
