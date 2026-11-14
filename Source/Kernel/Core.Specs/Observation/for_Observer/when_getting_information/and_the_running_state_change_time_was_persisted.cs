// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_getting_information;

public class and_the_running_state_change_time_was_persisted : given.an_observer
{
    readonly DateTimeOffset _changedAt = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
    ObserverInformationSnapshot _result;
    ObserverDefinition _definition;
    ObserverState _state;

    async Task Establish()
    {
        _persistedRunningStateChangedAt = _changedAt;
        await Crash();
    }

    async Task Because()
    {
        _definition = await _observer.GetDefinition();
        _state = await _observer.GetState();
        _result = await _observer.GetInformation();
    }

    [Fact] void should_return_the_definition() => _result.Definition.ShouldEqual(_definition);
    [Fact] void should_return_the_state() => _result.State.ShouldEqual(_state);
    [Fact] void should_not_be_subscribed() => _result.IsSubscribed.ShouldBeFalse();
    [Fact] void should_return_the_persisted_running_state_change_time() => _result.RunningStateChangedAt.ShouldEqual(_changedAt);
}
