// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Storage.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation;
using Orleans.Serialization.Cloning;

namespace Cratis.Chronicle.Setup.Serialization.for_SharedInstanceCopier;

public class when_copying : Specification
{
    SharedInstanceCopier _copier;
    EventType _eventType;
    ObserverReaderScope _scope;
    object? _copiedEventType;
    object? _copiedScope;

    void Establish()
    {
        _copier = new();
        _eventType = new("type", 1);
        _scope = ObserverReaderScope.Explicit([_eventType]);
    }

    void Because()
    {
        _copiedEventType = _copier.DeepCopy(_eventType, default!);
        _copiedScope = _copier.DeepCopy(_scope, default!);
    }

    [Fact] void should_hand_back_the_event_type() => ReferenceEquals(_copiedEventType, _eventType).ShouldBeTrue();
    [Fact] void should_hand_back_the_reader_scope() => ReferenceEquals(_copiedScope, _scope).ShouldBeTrue();
    [Fact] void should_support_event_types() => _copier.IsSupportedType(typeof(EventType)).ShouldBeTrue();
    [Fact] void should_support_the_observer_subscription() => _copier.IsSupportedType(typeof(ObserverSubscription)).ShouldBeTrue();
    [Fact] void should_support_job_states_and_the_types_derived_from_them() => _copier.IsSupportedType(typeof(JobState)).ShouldBeTrue();
    [Fact] void should_not_support_other_types() => _copier.IsSupportedType(typeof(ConnectedClientLike)).ShouldBeFalse();

    sealed class ConnectedClientLike;
}
