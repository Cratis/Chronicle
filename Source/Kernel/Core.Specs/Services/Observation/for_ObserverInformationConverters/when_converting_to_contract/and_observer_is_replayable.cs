// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Primitives;
using Cratis.Chronicle.Storage.Observation;
using ProtoBuf;

namespace Cratis.Chronicle.Services.Observation.for_ObserverInformationConverters.when_converting_to_contract;

public class and_observer_is_replayable : Specification
{
    ObserverInformation _result;
    Proto3ObserverInformation _proto3Result;
    ObserverDefinition _definition;
    ObserverState _state;

    void Establish()
    {
        _definition = new(
            "observer-1",
            [new EventType("some-event-type", 1)],
            EventSequenceId.Log,
            Concepts.Observation.ObserverType.Reactor,
            Concepts.Observation.ObserverOwner.Client,
            true);

        _state = new(
            "observer-1",
            EventSequenceNumber.First,
            Concepts.Observation.ObserverRunningState.Active,
            new HashSet<Concepts.Keys.Key>(),
            new HashSet<Concepts.Keys.Key>(),
            [],
            0,
            false,
            false)
        {
            NextEventSequenceNumber = 42,
            TailEventSequenceNumber = 99
        };
    }

    void Because()
    {
        using var payload = new MemoryStream();
        Serializer.Serialize(payload, _definition.ToContract(_state));
        payload.Position = 0;
        _result = Serializer.Deserialize<ObserverInformation>(payload);
        payload.Position = 0;
        _proto3Result = Serializer.Deserialize<Proto3ObserverInformation>(payload);
    }

    [Fact] void should_set_is_replayable_to_true() => _result.IsReplayable.ShouldBeTrue();
    [Fact] void should_send_explicit_true_to_proto3_clients() => _result.IsReplayableValue.ShouldEqual(BooleanValue.True);
    [Fact] void should_keep_the_legacy_proto3_read_behavior() => _proto3Result.IsReplayable.ShouldBeFalse();
    [Fact] void should_override_the_proto3_false_default() => _proto3Result.IsReplayableValue.Resolve(_proto3Result.IsReplayable).ShouldBeTrue();
    [Fact] void should_have_correct_id() => _result.Id.ShouldEqual(_definition.Identifier.Value);
    [Fact] void should_have_correct_running_state() => _result.RunningState.ShouldEqual(ObserverRunningState.Active);
    [Fact] void should_have_correct_tail_event_sequence_number() => _result.TailEventSequenceNumber.ShouldEqual(99ul);

    /// <summary>
    /// This decoder has ordinary proto3 defaults, not the legacy protobuf-net true default.
    /// </summary>
    [ProtoContract]
    class Proto3ObserverInformation
    {
        [ProtoMember(10)]
        public bool IsReplayable { get; set; }

        [ProtoMember(13)]
        public BooleanValue IsReplayableValue { get; set; }
    }
}
