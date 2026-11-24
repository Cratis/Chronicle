// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Diagnostics.Load.for_GrainTargets;

public class when_resolving_the_event_store : Specification
{
    string _manager;
    string _observer;
    string _eventSequence;
    string _unknownGrain;
    string _shortKey;
    string _lastSegment;

    void Because()
    {
        _manager = GrainTargets.EventStoreOf("IProjectionsManager", "the-store");
        _observer = GrainTargets.EventStoreOf("IObserver", "observer-id#the-store#the-namespace#event-log");
        _eventSequence = GrainTargets.EventStoreOf("IEventSequence", "event-log#the-store#the-namespace");
        _unknownGrain = GrainTargets.EventStoreOf("IJobsManager", "the-store#the-namespace");
        _shortKey = GrainTargets.EventStoreOf("IObserver", "observer-id");
        _lastSegment = GrainTargets.EventStoreOf("IWebhook", "webhook-id#the-store");
    }

    [Fact] void should_use_the_whole_key_of_a_manager() => _manager.ShouldEqual("the-store");
    [Fact] void should_use_the_second_segment_of_an_observer_key() => _observer.ShouldEqual("the-store");
    [Fact] void should_use_the_second_segment_of_an_event_sequence_key() => _eventSequence.ShouldEqual("the-store");
    [Fact] void should_use_the_last_segment_when_it_is_the_event_store() => _lastSegment.ShouldEqual("the-store");
    [Fact] void should_not_guess_for_a_grain_whose_key_is_not_known() => _unknownGrain.ShouldBeEmpty();
    [Fact] void should_give_nothing_when_the_key_is_too_short() => _shortKey.ShouldBeEmpty();
}
