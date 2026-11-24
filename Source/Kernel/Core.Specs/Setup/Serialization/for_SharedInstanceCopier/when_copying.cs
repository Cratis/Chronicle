// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Storage.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Setup.Serialization.for_SharedInstanceCopier;

public class when_copying : Specification
{
    SharedInstanceCopier _copier;
    JobState _jobState;
    object? _copiedJobState;

    void Establish()
    {
        _copier = new();
        _jobState = new();
    }

    void Because() => _copiedJobState = _copier.DeepCopy(_jobState, default!);

    [Fact] void should_hand_back_the_job_state() => ReferenceEquals(_copiedJobState, _jobState).ShouldBeTrue();
    [Fact] void should_support_job_states_and_the_types_derived_from_them() => _copier.IsSupportedType(typeof(JobState)).ShouldBeTrue();
    [Fact] void should_leave_event_types_to_the_immutable_attribute() => _copier.IsSupportedType(typeof(EventType)).ShouldBeFalse();
    [Fact] void should_leave_the_observer_subscription_to_the_immutable_attribute() => _copier.IsSupportedType(typeof(ObserverSubscription)).ShouldBeFalse();
    [Fact] void should_not_support_other_types() => _copier.IsSupportedType(typeof(ConnectedClientLike)).ShouldBeFalse();

    sealed class ConnectedClientLike;
}
