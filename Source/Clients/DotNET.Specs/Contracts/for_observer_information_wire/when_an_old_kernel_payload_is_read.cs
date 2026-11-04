// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Chronicle.Contracts.for_observer_information_wire;

public class when_an_old_kernel_payload_is_read : given.an_observer_information_payload
{
    ObserverInformation _result;

    void Because() => _result = Deserialize<ObserverInformation>(Serialize(CreateOldInformation()));

    [Fact] void should_leave_the_timestamp_unknown() => _result.RunningStateChangedAt.ShouldBeNull();
    [Fact] void should_keep_the_identifier() => _result.Id.ShouldEqual("observer");
    [Fact] void should_keep_the_event_sequence() => _result.EventSequenceId.ShouldEqual("event-log");
    [Fact] void should_keep_the_observer_type() => _result.Type.ShouldEqual(ObserverType.Reactor);
    [Fact] void should_keep_the_owner() => _result.Owner.ShouldEqual(ObserverOwner.Client);
    [Fact] void should_keep_the_event_types() => _result.EventTypes.Single().Id.ShouldEqual("event");
    [Fact] void should_keep_the_next_position() => _result.NextEventSequenceNumber.ShouldEqual(42UL);
    [Fact] void should_keep_the_last_position() => _result.LastHandledEventSequenceNumber.ShouldEqual(41UL);
    [Fact] void should_keep_the_running_state() => _result.RunningState.ShouldEqual(ObserverRunningState.Disconnected);
    [Fact] void should_keep_the_subscription() => _result.IsSubscribed.ShouldBeTrue();
    [Fact] void should_keep_replayability() => _result.IsReplayable.ShouldBeTrue();
    [Fact] void should_keep_the_tail() => _result.TailEventSequenceNumber.ShouldEqual(99UL);
    [Fact] void should_keep_the_handled_count() => _result.HandledEventCount.ShouldEqual(17UL);
}
