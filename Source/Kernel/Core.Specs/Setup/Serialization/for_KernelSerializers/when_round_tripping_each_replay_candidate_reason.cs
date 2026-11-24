// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation.Replaying;

namespace Cratis.Chronicle.Setup.Serialization.for_KernelSerializers;

public class when_round_tripping_each_replay_candidate_reason : given.the_kernel_serializers
{
    ReplayCandidateReason[] _sent;
    ReplayCandidateReason[] _received;

    void Establish() => _sent =
    [
        new ProjectionDefinitionChangedReplayCandidateReason(),
        new ProjectionSelectiveReplayAvailableReplayCandidateReason(),
        new ProjectionUpdateAvailableReplayCandidateReason(),
        new ReactorDefinitionChangedReplayCandidateReason(),
        new ReducerDefinitionChangedReplayCandidateReason(),
        new RetiredProjectionSharedContainerReplayCandidateReason(),
        new WebhookDefinitionChangedReplayCandidateReason(),
        new EventTypesChangedReplayCandidateReason([new EventType("first", 1)], [new EventType("second", 2)])
    ];

    void Because() => _received = [.. _sent.Select(reason => (ReplayCandidateReason)SendBetweenSilos(typeof(ReplayCandidateReason), reason)!)];

    [Fact] void should_keep_the_type_of_every_reason() => _received.Select(reason => reason.GetType()).ShouldEqual(_sent.Select(reason => reason.GetType()));
    [Fact] void should_keep_the_reason_type_of_every_reason() => _received.Select(reason => reason.Type).ShouldEqual(_sent.Select(reason => reason.Type));
    [Fact] void should_keep_the_event_types_that_changed() => ((EventTypesChangedReplayCandidateReason)_received[^1]).NewEventTypes.Single().Id.Value.ShouldEqual("second");
}
