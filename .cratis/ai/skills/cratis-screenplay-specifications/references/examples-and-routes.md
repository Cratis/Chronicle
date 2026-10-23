<!-- cratis-ai-managed: skills/cratis-screenplay-specifications/references/examples-and-routes.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Typed examples, overrides and event routes

## Contents

- Typed examples
- Examples typed from another document
- Effective values
- Event routes
- Redelivery locators
- Operation and recovery intent
- Givens without an event source

## Typed examples

`example <Name> : <EventOrCommandOrReadModel>` declares a possibly partial typed
fixture at document/module/feature/slice scope. Its underlying type resolves in
the declaration scope, not the use scope. It can state properties, description,
`for` and command generated fixtures, not callers/clocks or a whole scenario.
Examples share the type namespace and use the current event generation. They
cannot inherit from examples or serve as query results/returns/trigger/capture
fixtures. They are specification data, not operational `seed` events.

This complete example compiles and its two reference scenarios pass:

```screenplay
concept ProjectId : Uuid
module Projects
  feature Registration
    example DefaultProject : RegisterProject
      name = "Default"
    slice StateChange Register
      command RegisterProject
        projectId ProjectId identifier
        name String
        produces event ProjectRegistered
          name String = name
      specification RegisteringTheDefault
        when DefaultProject
          projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
        then ProjectRegistered
          for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
          name = "Default"
      specification OverridingTheName
        when DefaultProject name = "Override"
          projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
        then ProjectRegistered
          for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
          name = "Override"
```

## Examples typed from another document

An example's type may be an event, command or read model declared in another
document of the same compilation, by simple name when unambiguous or by qualified
name (`Billing.Facts.Recording.Recorded`). Checked against the standalone compiler:

```text
// declarations.play: module Billing > feature Facts > slice StateChange Recording
//   declares command Record, event Recorded and a specification using the examples
// application.play
import "declarations.play"

example Fact : Billing.Facts.Recording.Recorded
  amount = 10
  for "one"
example Input : Record
  id = "one"
  amount = 20
```

That folder compiled with warnings as errors and `screenplay test` passed the
specification that uses `given Fact` and `when Input`. Rules:

- A document checked on its own defers an imported type, as for an inline
  `given`; it reports no `PLAY0520` there.
- Values are checked only when the compilation includes the declaration. A wrong
  value (a string for an `Int`) surfaces as `PLAY0525` at binding (`screenplay
  test`), not at compile.
- A type that is still unknown or ambiguous after assembly, or names a concept,
  type or another example, is `PLAY0520`. A scratch file importing only the
  contract name is exactly that case:

```screenplay expect PLAY0520
import Billing.Facts.Recording.Recorded
example Fact : Recorded
  amount = 10
```

- Import the declaring file (`import "declarations.play"`), not the contract
  name. A named `import X.Y.Z` of a declaration the application already has warns
  `PLAY0290` (no effect), and binding still refuses any named import with
  `PLAY0268`, as for inline givens; public `from "<store>"` imports are refused
  the same way.
- The example is resolved in its own declaration scope, so shared example data may
  live beside the declarations it types, in any document.

## Effective values

An example occupies the ordinary name slot in given/event, given readmodel,
when command, when append, then event or then readmodel. One assignment may be
inline after the name; more stay indented. There is **no with keyword**.
Step values override example values by property name; objects/lists replace
whole values, never recursively merge. Indented `for` replaces its destination;
generated fixtures merge by name. A new step-only value is authored, not an
override. Each step/declaration assigns a property once (PLAY0519); malformed
headers are PLAY0518.

After expansion, required given-event/read-model, command, append and expected
event fields must be complete (binding PLAY0524); examples can remain partial.
Read-model assertions retain subset matching unless `exactly` is authored.
Even unused example values or values overridden at every use are type-checked.
No default fixture values are invented. A command action must belong to the
specification's own slice even when a qualified example resolves elsewhere.

MCP `find-fixtures` reports effective authored/example/override origins and
replaced values. Source-bound runner failures retain that provenance; canonical
ESM contains merged values only, equivalent to hand-expansion. ESM-only runners
cannot reconstruct source origins.

## Event routes

Event occurrences in `given`, `when append` and `then` accept `stream Source.Stream`
with nested scalar `streamId = <literal>` or a complete composite part block.
These select ESM v8 and reference-execute; the complete checked model
is in toolchain `references/sources-and-streams.md`. Command actions take their
route from the command declaration, never the fixture.

Routed history/appends need typed concrete `for`; routed expectations may omit
it. `then ... no stream` expects no route; no route in a then is a wildcard.
`no stream` is invalid on given/appends. Event examples can supply routes, but
steps replace the entire route, independently of `for`; an inherited route
cannot be changed back to a wildcard. Literal compatibility/route contradiction
checks use resolved source/stream types, not guessed text (PLAY0547–PLAY0551).
Route literals must be nonempty, well-formed NFC text
(refused, not normalized); Double integer ids are within ±(2^53−1), while Exact
has no such bound but the `numbers exact` source mode remains unadmitted (PLAY0268; exact lowering of
qualifying Double literals is the separate, admitted v11). Invalid specification routes report
PLAY0549. Contradictions compare canonical scalar ids and every composite part,
so UUID casing is immaterial. Composite mappings supply each named part once,
using literals only; encoding escapes `%`/`|` and preserves declaration order.
Source: `event-sources.md`, `specifications.md`, `diagnostics.md`.

A renderer that stops at v7 refuses a model above it; the CLI/Stage pair
(`cratis-screenplay-toolchain/references/renderable-subset.md`) reaches route checks through v10. Routed read-model/query replay can still refuse
STAGE-ESM-030. Source/reference success never establishes target parity.

Command and read-model examples refuse routes (PLAY0526). Effective role and
`for` checks use the inherited or replaced route: inherited `no stream` on a
`given`/append is PLAY0547; incompatible destination/route pairs are PLAY0550,
and contradictory expectations PLAY0551. Declaration checks still run on unused
examples and routes overridden at every use. MCP route rows retain the origin
(authored/example/override), with the replaced whole route on the stream/no-stream
row and individual lineage on id parts.

## Redelivery locators

`when redelivered <Event> to <Reaction>` may contain a route, using the same scalar
or complete composite `streamId` shape as event steps. No route is a wildcard;
`no stream` selects the unrouted given. Compare effective, example-expanded givens.
Every `for`, payload and route comparison is true, false or unknown: a definite
mismatch excludes a candidate even when another comparison is unknown. Selection
needs exactly one definitely matching candidate and **no undecided candidates**
(PLAY0543). Equal duplicate givens remain separate candidates. Narrow with `for`,
values, `stream` or `no stream`; do not guess an unknown value. A locator's `for`
is optional but typed against the routed source when present.

This complete model validates and `screenplay test <root>` exits 0 with one passed redelivery scenario.

Requires Screenplay 4.157.0 or later (`screenplay --version`) for redelivery and no-event assertions (ESM v17); older compilers report PLAY0268.

```screenplay
eventsource Account
  identifier String
  stream Transactions
    streamId String
module Accounts
  feature Recovery
    slice Automation Observe
      event Recorded
        amount Int
      reaction Observer
        when Recorded
      example FirstPartition : Recorded
        for "account-1"
        stream Account.Transactions
          streamId = "first"
        amount = 10
      specification RedeliveringTheSecondPartition
        given FirstPartition
        given FirstPartition
          stream Account.Transactions
            streamId = "second"
        when redelivered Recorded to Observer
          stream Account.Transactions
            streamId = "second"
        then no events
```

The second given replaces the example's **entire** route, independently of `for`,
not just its id. PLAY0548 is command-action-only, never a locator route error;
malformed routes use PLAY0547 and resolution/key-shape errors PLAY0549. MCP labels
locator fixtures `whenRedeliveredEvent` with route-origin rows. Delivery fires only
the named reaction, then settles the cascade without appending the input again.

## Operation and recovery intent

Operation failure/request/compensation forms are authoring-only (PLAY0268):
`given operation <Name> fails`, `then operation <Name>` with a partial input
subset, and leaf `then compensated <Name>`. See command-surface
`references/operations.md` for the complete checked authoring fixture.

Refusal selectors bind/reference-execute as ESM v19; redelivery to one observer
and `then no events` as v17. Forms, matching, acknowledgement and the distinction
between domain refusals and infrastructure failures are in captures-and-reactions
`references/refusals-and-redelivery.md`.

Authority: Screenplay `Documentation/screenplay/specifications.md`.

`given operation ... fails`, `then operation` and `then compensated` describe
future external-work behavior. Use `then no events` to assert explicitly that the
action and cascade produced no new facts; after append it excludes the input fact.
It may accompany view/query/response assertions, not events, event-order assertions,
errors or denial (PLAY0545). An omitted event list is not this explicit assertion.

## Givens without an event source

A `given` event with no `for` belongs to the event source the specification acts on: the
event source each fact the command appends goes to, or the `for` of `when append`.

- A unique-event constraint counts a sourceless given as already on that event source. A
  unique-value constraint treats its value as the action's own claim.
- When a projection needs the given's event-source identity, the reference evaluator reports
  `Unsupported` (Projection) and does not invent one. This holds whatever the action,
  because givens are projected before it runs.
- Runners that append to real event streams, such as Stage, append sourceless givens to the
  action's event source.
- State `for` when the command appends to more than one event source, or when an assertion
  depends on the given's event source. Never assert `$eventSourceId` of a sourceless given.

Requires Screenplay 4.229.0 (`screenplay --version`) for the documented, vector-pinned
placement; older versions leave a sourceless given's placement unspecified.
