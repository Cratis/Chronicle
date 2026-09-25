// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Auditing;
using ProtoBuf;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending;

public class and_events_mix_explicit_and_ambient_causation : given.an_event_sequence_with_metadata
{
    Causation _ambient;
    Causation _explicit;
    Contracts.Sequences.AppendManyForEventSourcesRequest _roundTrippedRequest;

    void Establish()
    {
        _ambient = new(DateTimeOffset.UnixEpoch, "ambient", new Dictionary<string, string> { ["marker"] = "ambient" });
        _explicit = new(DateTimeOffset.UnixEpoch, "explicit", new Dictionary<string, string> { ["marker"] = "explicit" });
        _causationManager.GetCurrentChain().Returns(ImmutableList.Create(_ambient));
    }

    async Task Because()
    {
        await _eventSequence.AppendMany([
            new(_source, "first"),
            new(_source, "second", _explicit)
        ]);
        _roundTrippedRequest = Serializer.DeepClone(_batchRequest);
    }

    [Fact] void should_keep_the_ambient_chain_for_the_batch() => _batchRequest.Causation.ToClient().ShouldEqual([_ambient]);
    [Fact] void should_not_inherit_the_other_events_explicit_causation() => _batchRequest.Events.First().Causation.ShouldBeNull();
    [Fact] void should_send_ambient_then_explicit_for_the_second_event() => _batchRequest.Events.Last().Causation.ToClient().ShouldEqual([_ambient, _explicit]);
    [Fact] void should_round_trip_the_batch_causation_through_protobuf() => _roundTrippedRequest.Causation.ToClient().Select(_ => _.Type.Name).ShouldEqual(["ambient"]);
    [Fact] void should_leave_the_first_events_causation_unset_through_protobuf() => _roundTrippedRequest.Events.First().Causation.ShouldBeNull();
    [Fact] void should_round_trip_the_second_events_causes_through_protobuf() => _roundTrippedRequest.Events.Last().Causation.ToClient().Select(_ => _.Type.Name).ShouldEqual(["ambient", "explicit"]);
    [Fact] void should_round_trip_the_second_events_properties_through_protobuf() => _roundTrippedRequest.Events.Last().Causation.ToClient().Select(_ => _.Properties["marker"]).ShouldEqual(["ambient", "explicit"]);
    [Fact] void should_notify_first_event_with_ambient_only() => _notifications[0].Event.Context.Causation.ShouldEqual([_ambient]);
    [Fact] void should_notify_second_event_with_both_causes() => _notifications[1].Event.Context.Causation.ShouldEqual([_ambient, _explicit]);
}
