<!-- cratis-ai-managed: skills/cratis-screenplay-toolchain/references/production-routes-and-filters.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Per-production routes and observer filters (released ESM v10)

## Contents

- Command defaults and complete overrides
- Observer selection
- Checked source form
- Admission and evidence

## Command defaults and complete overrides

Named event sources/streams and command routes bind as ESM v8 (verified at 4.125.0).
A routed command's identifier and known destinations must match the source's nominal
identifier type (PLAY0504). Allocation uses the generated identifier's type, or
without one requires a UUID-based source. Composite `streamId` routes map every
named part exactly once; scalar `streamId T` and `streamId = x` remain valid.
See [sources and streams](sources-and-streams.md) for declarations and encoding.

A command's plain, conditional or inline event production can carry its own
`stream Source.Stream` plus scalar or composite `streamId` mappings. This replaces
the **whole source, stream and stream id**, for that production only. Other
productions inherit the command route. Overrides do not supply `for`: state the
destination explicitly and keep its type compatible with the override's source.
There is no `no stream` override. A payload `stream = value` remains payload.
Reaction and refusal-branch productions cannot carry routes (PLAY0663).

Resolve the command route and every override eagerly after validation/requirements
and before generation. An invalid route in a skipped conditional production still
rejects the command atomically as Contract, leaving no new facts or allocation.
Mappings accept required nongenerated direct scalar command inputs or literals;
property paths remain PLAY0268 (#574), generated inputs PLAY0273. A portable
unkeyed override replaces a keyed command id, never inherits it; a renderer must
refuse that form if its adapter cannot clear inherited metadata.

## Observer selection

A reaction or reducer has at most one leaf `from Source` or `from Source.Stream`.
It matches stored source/stream names, not stream ids. Unrouted facts never match;
without a filter, existing observation behavior is unchanged. Direct reaction
productions, captures and public publication are unrouted. A filtered reaction may
only have `when Event` triggers on declared events, not clock/application triggers.
Filters are not available on projections or captures.

PLAY0665 rejects malformed, duplicate, child-bearing, unresolved/ambiguous filters
and non-event triggers. PLAY0666 warns when all statically known producers lie
outside the filter: include effective production overrides in this check. Handler
commands and foreign origins are unknown and suppress this C#-only warning.
A filtered opaque reducer returns typed Unsupported when matching given history
or the when-step reaches a transition; ignoring its body cannot prove absence.

## Checked source form

The override and filter select ESM v10. The second production keeps the command
default, while Booked is routed to Notes and can trigger Follow:

```screenplay
concept AccountId : String
eventsource Account
  identifier AccountId
  stream Base
    streamId String
  stream Notes
    streamId String
module Accounts
  feature Booking
    slice StateChange Book
      command Book
        accountId AccountId identifier
        period String
        stream Account.Base
          streamId = period
        produces event Booked
          for accountId
          stream Account.Notes
            streamId = period
        produces event BookRecorded
          for accountId
    slice Automation FollowBooking
      event Followed
      reaction Follow
        from Account.Notes
        when Booked
          produces Followed
      specification RoutedReaction
        when append Booked
          for "account-1"
          stream Account.Notes
            streamId = "2026-10"
        then Followed
          no stream
```

For a composite override, the child form is `streamId` followed by every
`part = inputOrLiteral`. Mapping order does not change part correspondence.
Specification routes describe effective facts, not only the command default.
Cover inherited versus overridden routes, atomic skipped-route failure, matching
and wrong source/stream, and unrouted input. Read [specification route forms](sources-and-streams.md).

## Admission and evidence

ESM v10 was first published in 4.125.0 (reaction system identity). Production
routes and observer filters shipped in 4.129.0. Decision 0066 records that already
shipped extension as a retroactive exception; all three are now one frozen v10
contract. A consumer admitting v10 must include all three, not just identity.
Composite **read-model** keys are separate and remain unadmitted (PLAY0268, #599).

Previous CLI 3.46.1/Stage 4.52.0 refused versions above v7 before planning.
The pinned CLI/Stage pair (`versions.md`) admits through v15 to construct checks: production overrides
still refuse STAGE-ESM-036; reaction/reducer filters render on the semantic path,
while legacy syntax paths retain STAGE-ESM-035.
Source: Stage `v4.89.1:Source/Rendering.Cratis/Semantics/SemanticVersionFeatureAdmission.cs:27-34`,
`Documentation/reference/reaction-rendering.md:33-34,83-98`.
A supported version number alone never proves target support or a passing render.

Source: Screenplay `v4.135.0:Documentation/screenplay/{event-sources,readmodels,reactions,diagnostics}.md`,
decisions 0061–0063/0066 and canonical corpus
`Source/DotNET/Screenplay.CanonicalCorpus/Corpus/EventRoutes/{production-routes,observer-filters}/source/`.
Stage `v4.67.0:Source/Rendering.Cratis/Semantics/{SemanticVersionFeatures,SemanticVersionFeatureAdmission}.cs`;
CLI `v3.50.0:Source/Cli/Commands/Render/RenderedSemanticVersions.cs`.
