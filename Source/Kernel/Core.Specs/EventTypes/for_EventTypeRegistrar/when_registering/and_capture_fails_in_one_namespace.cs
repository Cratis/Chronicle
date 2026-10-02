// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Patterns;

namespace Cratis.Chronicle.EventTypes.for_EventTypeRegistrar.when_registering;

public class and_capture_fails_in_one_namespace : Patterns.for_PatternCapture.given.a_pattern_capture_with_a_failing_namespace
{
    Exception _error;

    void Establish()
    {
        _eventTypes.GetAllDefinitions().Returns([]);
        _eventTypes.Register(Arg.Any<IEnumerable<EventTypeToRegister>>()).Returns([new EventTypeId("CustomerNamed")]);
        _grainFactory.GetGrain<IEventSequence>(Arg.Any<string>()).Returns(Substitute.For<IEventSequence>());
    }

    async Task Because() => _error = await Catch.Exception(() => new EventTypeRegistrar(_grainFactory).Register(
        _eventStore,
        [new Contracts.Events.EventTypeRegistration { Type = new() { Id = "CustomerNamed", Generation = 1 }, Schema = "{}" }],
        false,
        _storage,
        Substitute.For<IEventTypesCacheClient>(),
        _capture));

    [Fact] void should_complete_registration() => _error.ShouldBeNull();
    [Fact] async Task should_have_attempted_the_failing_namespace() => await _failedObserver.Received(1).SubscribeAdditively<IPatternCaptureSubscriber>(ObserverType.Reactor, Arg.Any<IEnumerable<EventType>>(), SiloAddress.Zero, isReplayable: false);
    [Fact] async Task should_subscribe_the_remaining_namespace() => await _observer.Received(1).SubscribeAdditively<IPatternCaptureSubscriber>(ObserverType.Reactor, Arg.Any<IEnumerable<EventType>>(), SiloAddress.Zero, isReplayable: false);
}
