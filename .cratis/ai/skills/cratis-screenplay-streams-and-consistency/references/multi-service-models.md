<!-- cratis-ai-managed: skills/cratis-screenplay-streams-and-consistency/references/multi-service-models.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Modeling a system of several services

## Contents

- One model folder per service
- A module is not a store boundary
- The contract between two service models
- Diagnostics at the service boundary
- Tooling: one workspace and one verdict set per service
- Rendering: one application per run
- What nothing checks

## One model folder per service

Screenplay has no `service`, `event store` or `deployment` construct. A folder of
`.play` files is exactly one application, and one application becomes one Chronicle
event store when rendered. A system of several services, each owning its own store,
is therefore several model folders:

```text
Shop/
├── Ordering/Screenplay/        # domain Shop.Ordering; .screenplay/identities.json
└── Billing/Screenplay/         # domain Shop.Billing; .screenplay/identities.json
```

- Each folder has its own `domain`, its own `.screenplay/identities.json` and its
  own concepts. A concept both services use (`OrderId`) is declared in both.
- Never put the services under one folder that is compiled or rendered as a whole;
  the files merge into one application, and a second `domain` in that folder
  refuses with `PLAY0172`.
- `eventsource` declares an event-source type inside one store and `system` an
  external system an operation uses; neither expresses a service or a store.

## A module is not a store boundary

Modules inside one application share its single event store. Use modules for
bounded contexts that ship together in one service; reference another module's
events by name, and state the dependency with `depends on <Module>` or
`depends on <Module>.<Feature>` when the team opts in. Once a container declares
dependencies, an undeclared counted reference into a producer module is `PLAY0552`
(warning) and a declared dependency nothing uses is `PLAY0553` (information).
`depends on` cannot name another application. Split a module into its own model
folder only when it gets its own store and deployment.

## The contract between two service models

The producer declares a `public event` in an outbound `Translate` slice and builds
it from private events with an event-target projection. The consumer redeclares
the same shape as `event <Name> from "<producer store>"` in an inbound `Translate`
slice and captures it into a private event of its own. The quoted origin is the
producer's Chronicle event-store name, the `--name` its model is rendered with.

Producer, `Ordering/Screenplay/ordering.play`:

```screenplay
domain Shop.Ordering
concept OrderId : Uuid
module Ordering
    feature Placing
        slice StateChange PlaceOrder
            command PlaceOrder
                orderId OrderId identifier
                total Decimal
                produces OrderPlaced
                    for orderId
                    total = total
            event OrderPlaced
                total Decimal
        slice Translate PublishOrderAccepted
            direction outbound
            public event OrderAccepted
                orderId OrderId
                total Decimal
            projection OrderAcceptedPublisher => OrderAccepted
                from OrderPlaced
                    orderId = $eventSourceId
                    total = total
            specification PublishingAPlacedOrder
                when append OrderPlaced
                    for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                    total = 10
                then OrderAccepted
                    orderId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                    total = 10
```

Consumer, `Billing/Screenplay/billing.play`:

```screenplay
domain Shop.Billing
concept OrderId : Uuid
module Billing
    feature Invoicing
        slice Translate AcceptOrders
            direction inbound
            event OrderAccepted from "Ordering"
                orderId OrderId
                total Decimal
            event InvoiceRequested
                total Decimal
            capture OrderIntake
                source events
                    from OrderAccepted
                key orderId
                append InvoiceRequested
                    total = $.total
```

- Name the public contract differently from the private fact it summarizes
  (`OrderAccepted` from `OrderPlaced`). One application cannot hold a producer's
  public event and a foreign event of the same name (`PLAY0472` with `PLAY0268`),
  another reason each service has its own folder.
- A capture keys on the incoming content, so the contract carries the identity
  the consumer keys on.
- The producer's publication specification runs in the reference evaluator; the
  consumer's capture over events binds but evaluates as typed Unsupported. Specify
  the consumer's private behavior downstream of the capture.
- Syntax, publication semantics and the full rules: read
  `cratis-screenplay-automations-and-translations`
  `references/public-events-and-direction.md`.

Requires Screenplay 4.117.0 or later (`screenplay --version`) for public events,
origins and direction (ESM v9).

## Diagnostics at the service boundary

| Code | Fix |
| --- | --- |
| `PLAY0607` | A command produced a public event; publish from an outbound Translate instead |
| `PLAY0608`, `PLAY0609`, `PLAY0617` | An outbound slice publishes exactly one local public event type, and only there |
| `PLAY0610`, `PLAY0616` | A foreign event is consumed only in an inbound Translate, and an inbound slice consumes only foreign events |
| `PLAY0611`, `PLAY0612`, `PLAY0613` | Outbound consumes private local events; inbound produces private local events; a foreign event is never produced locally |
| `PLAY0614` | A Translate that declares or uses public metadata states its direction |
| `PLAY0615`, `PLAY0618`, `PLAY0619`, `PLAY0620` | Captures of outside data stay inbound, event-target builders stay outbound, and `source events` lists distinct `from <Event>` lines |
| `PLAY0268` | A public or foreign `import` has no local shape; declare `event X from "Store"` with its fields |

One capture reads one origin: split a capture whose `from` events come from several
origins into one capture per origin (`PLAY0687`). Chronicle realizes one capture as one inbox observer and refuses a mixed capture at runtime.

Requires Screenplay 4.155.0 or later for `PLAY0687`; older compilers accept a mixed capture silently, so check origins by review there.

## Tooling: one workspace and one verdict set per service

- Compile, test and record verdicts V1–V5 per model folder
  (`cratis-screenplay-toolchain` owns the commands). A system-level verdict does
  not exist; the contract agreement below is a review step.
- The `screenplay` MCP server works on one workspace at a time; switching clears
  outstanding proposals. When `cratis screenplay mcp` locates the project's model
  itself (`--project-root`, or `.cratis/ai.json` without
  `mcpServers.screenplay.root`), it serves the common folder of the project's
  `.play` files, which for several service folders is their parent and merges the
  services. Start each connection with the service's model folder as its explicit
  path (`cratis screenplay mcp Ordering/Screenplay`).
- `cratis screenplay generate` describes one application per solution or solution
  filter. Generate each service from its own `.slnf` into its own model folder. A
  reactor observing anything other than the event log, such as another store's
  inbox, keeps its code reference instead of becoming an inbound Translate; model the
  Translate by hand.

## Rendering: one application per run

- Render each service separately:
  `cratis render Ordering/Screenplay --name Ordering --destination Ordering/out`,
  then the same for `Billing`. `--name` is the application identity and becomes the
  Chronicle event-store name and the MongoDB database name, so it must equal every
  consumer's `from "…"` origin.
- Stage refuses public events and event-target builders (`STAGE-ESM-032`), foreign
  events and events-source captures (`STAGE-ESM-033`) and directed Translate slices
  (`STAGE-ESM-024`). Render the admitted modules with `--scope` and gap-fill the
  publication and the consumer translation by hand against the model, using
  `cratis-multi-service-architecture` for the runtime shape
  (`cratis-screenplay-render-and-gap-fill` owns the workflow).
- Each rendered scaffold carries its own single-kernel compose file with fixed host
  ports. Run one kernel for all services instead
  (`cratis-multi-service-local-orchestration`).

## What nothing checks

- The consumer's copy of a contract against the producer's: field names, types and
  the event name agree only by review. Keep the consumers named in the public
  event's `documentation`, and diff both declarations whenever either changes.
- The origin string against a real store or model: a typo binds silently.
- Contract versions across services: generations, `id` pins and stored names apply
  within one application. Treat a public event as a published API and make a breaking
  change a new event (`references/evolution.md`).
