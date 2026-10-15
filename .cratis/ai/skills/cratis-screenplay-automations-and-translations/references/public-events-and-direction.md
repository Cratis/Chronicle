<!-- cratis-ai-managed: skills/cratis-screenplay-automations-and-translations/references/public-events-and-direction.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Public events and translation direction (released ESM v9)

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
      event OrderDispatched
        orderId OrderId
      capture ShipmentTracking
        source events
          from ShipmentDispatched
        key orderId
        append OrderDispatched
          orderId = $.orderId
```

This compiles and binds as ESM v9. Evaluating the events-source capture returns
typed Unsupported: the event-payload-to-working-record contract is not admitted.
Do not equate binding with a passing capture specification or invent that mapping.
Keep correlation and resulting private events in the model and report the runtime gap.

## Admission and evidence

ESM v9 shipped in Screenplay 4.117.0 and is selected only by use of public events,
origins, direction, event-target builders or events-source captures. At 4.135.0,
publication reference scenarios execute; imported shapes still refuse binding.
Stage 4.52.0 (previous CLI 3.46.1, history) audited only through v7. The pinned CLI/Stage pair (toolchain `references/versions.md`)
reaches construct admission through v15 but still refuses publication (STAGE-ESM-032), foreign
events/events-source captures (STAGE-ESM-033); source admission is not rendering.

Source: Screenplay `v4.135.0:Documentation/screenplay/fixtures/public-events.play`,
`events.md`, `slices.md`, `captures.md`, decisions 0049–0051 and
`Source/DotNET/Screenplay.CanonicalCorpus/PublicEventsCorpus.cs`.
Renderer source: Stage `v4.52.0:Source/Rendering.Cratis/Semantics/SemanticCratisAdmission.cs`
and `v4.67.0:Source/Rendering.Cratis/Semantics/SemanticVersionFeatures.cs`.
Diagnostics and repairs: toolchain `references/diagnostics.md`; version allocation:
toolchain `references/versions.md`.
