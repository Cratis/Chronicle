<!-- cratis-ai-managed: skills/cratis-screenplay-automations-and-translations/references/public-events-and-direction.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Public events and translation direction

## Contents

- Choose the boundary and direction
- Outbound publication and specifications
- Inbound event captures
- Admission and evidence

## Choose the boundary and direction

Events are private by default. `public event X` declares a local public contract;
`event X from "store"` declares a foreign public event's shape locally. The quoted
origin is an opaque store name, not a path or declaration reference. Nothing checks
that the local shape matches the other application's contract. A public/foreign
`import Qualified.X from "store"` has no local shape and remains PLAY0268.

`direction inbound` and `direction outbound` belong directly inside a Translate
slice. Omission preserves historical inbound behavior for translations with no
public metadata. A translation declaring or using public metadata needs explicit
direction (PLAY0614), not a guess from its name.

- Inbound: accept public foreign events or outside data and produce private local
  facts. Use `capture` or a translating reaction. Correlation, deduplication,
  enrichment and recovery still need the method skill's decisions.
- Outbound: consume private local facts and publish exactly one local public event
  type through a reaction or event-target projection/reducer. No capture belongs
  here. A private-to-private follow-up is an Automation, not an outbound Translate.
- A command never produces a public event (PLAY0607), and a foreign public event
  is never produced locally (PLAY0613). Export only the intended contract fields;
  public visibility does not authorize exposing personal data or secrets.

## Outbound publication and specifications

`projection P => PublicEvent` resolves its target as an event rather than a read
model. Keep read-model and event names distinct. The outbound slice owns exactly
one local public event type; its builders target that event only. Event-target
reducers have the same boundary but opaque transitions: the reference evaluator
returns typed Unsupported when a transition is reached, not a successful fold.

An event-target projection holds one folded instance per event source id, not a
read-model key. It publishes when the folded instance is complete and differs from
the last published instance for that id. An identical repeated private fact emits
nothing; changed state emits a new public instance, never rewrites the old fact.
The reference fold admits only root-property set/clear from literals or event
properties; other mappings block planning. Publication is unrouted and reactions
can observe the new public fact. A route filter therefore cannot select publication.

This complete source uses the released publication specification form:

```screenplay
domain Contracts
concept OrderId : Uuid
concept OrderStatus : String
module Integration
  feature Translation
    slice StateChange PackOrder
      command PackOrder
        orderId OrderId identifier
        carrier String
        produces event OrderPacked
          carrier String = carrier
    slice Translate PublishOrderShipped
      direction outbound
      public event OrderShipped
        carrier String
        status OrderStatus
      projection OrderShippedPublisher => OrderShipped
        from OrderPacked
          status = "packed"
      specification PublishingAPackedOrder
        when append OrderPacked
          for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
          carrier = "DHL"
        then OrderShipped
          carrier = "DHL"
          status = "packed"
```

`when append <PrivateEvent>` commits the input; the `then <PublicEvent>` list
contains only the follow-up publications, exhaustive and ordered. Also specify
incomplete state, unchanged repeat, changed state and independent event source ids.
An expected public instance is not a `then readmodel` assertion.

## Inbound event captures

The foreign declaration must carry a quoted origin. `source events` has one or
more distinct `from <ForeignEvent>` children; no URL, table or other setting is
allowed in that block. It is valid only in an explicitly inbound Translate slice.

```screenplay
domain Contracts
concept OrderId : Uuid
module Integration
  feature Tracking
    slice Translate TrackShipments
      direction inbound
      event ShipmentDispatched from "shipping"
        orderId OrderId
        carrier String
      event ShipmentDelivered from "shipping"
        orderId OrderId
        status String
      event OrderDispatched
        carrier String
      event OrderDeliveryRecorded
        carrier String
        status String
      capture ShipmentTracking
        source events
          from ShipmentDispatched
          from ShipmentDelivered
        key orderId
        append OrderDispatched
          when added
            carrier = $.carrier
        append OrderDeliveryRecorded
          when status
            carrier = $.carrier
            status = $.status
      specification RecordingTheFirstDispatch
        when append ShipmentDispatched
          orderId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
          carrier = "DHL"
        then OrderDispatched
          for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
          carrier = "DHL"
      specification FoldingEarlierDispatchIntoDelivery
        given ShipmentDispatched
          orderId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
          carrier = "DHL"
        when append ShipmentDelivered
          orderId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
          status = "delivered"
        then OrderDeliveryRecorded
          for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
          carrier = "DHL"
          status = "delivered"
      specification IgnoringAnUnchangedDispatch
        given ShipmentDispatched
          orderId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
          carrier = "DHL"
        when append ShipmentDispatched
          orderId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
          carrier = "DHL"
        then no events
```

Requires Screenplay 4.171.0 or later (`screenplay --version`) for event-capture
reference execution and PLAY0700/0701; older runners do not deliver these inputs
to captures. Run `screenplay <model> --warnaserror`, then `screenplay test <model>`:
zero diagnostics and all three capture specifications passed are the pass signals.

Present the consumed public event itself: `given <Event>` / `when append <Event>`.
Fields are typed by its local declaration. `given capture` / `when capture` is
PLAY0701, with a repair only for exactly one `from` event (several are ambiguous).
`for` is optional on foreign fixtures: when supplied, it is opaque nonempty text
identifying the foreign source, not the capture key. A content key such as `orderId`
must be required on every `from` event (PLAY0700); `$eventSourceId` and other context
keys are refused. An empty or whitespace-only text key fails execution, even in a
given before the action; non-text/non-numeric keys are Unsupported.

Givens fold in authored order without appending. Each event overlays its top-level
properties onto state for that typed key: absent properties retain earlier values;
objects/lists replace whole values. First sight is `added` only, unchanged overlays
append nothing, and field/`and`/`or`/transition guards require a modification. An
append without `when` holds on any change, not on every delivery. Maps apply to
before and after states; `$.field` reads the mapped merged state. An expression
guard evaluates the after state only on a change. Root `when removed` never holds.

Captures run after reactions to the same input settle; each capture append settles
its own cascade before the next. Mixed capture/reaction assertions may need
`then events in any order`: reference order does not prove independent observer
order. Only `$context.occurred` (from `given clock`) is admitted; other context
members fail binding. `children`/`nested` over events remain Unsupported, as do
composite/list values used as scalars or watched by guards. Reference state never
persists between specifications and does not prove redelivery deduplication,
source acknowledgement, retry safety or context propagation. A passing reference
scenario is not renderer admission; Stage must refuse forms Chronicle cannot realize.

## Admission and evidence

ESM v9 is selected by use of public events, origins, direction, event-target
builders or events-source captures. Publication and admitted event-capture
reference scenarios execute; imported shapes still refuse binding.
Stage 4.52.0 (previous CLI 3.46.1, history) audited only through v7. The CLI/Stage pair (toolchain `references/renderable-subset.md`)
reaches construct admission through its bundled ceiling but still refuses publication (STAGE-ESM-032), foreign
events/events-source captures (STAGE-ESM-033); source admission is not rendering.

Requires Screenplay 4.117.0 or later (`screenplay --version`) for public events (ESM v9).

Source: Screenplay `Documentation/screenplay/fixtures/public-events.play`,
`events.md`, `slices.md`, `captures.md`, decisions 0049–0051 and
`Source/DotNET/Screenplay.CanonicalCorpus/PublicEventsCorpus.cs`.
Renderer source: Stage `Source/Rendering.Cratis/Semantics/SemanticCratisAdmission.cs`
and `Source/Rendering.Cratis/Semantics/SemanticVersionFeatures.cs`.
Diagnostics and repairs: toolchain `references/diagnostics.md`; version allocation:
toolchain `references/executable-subset.md`.
