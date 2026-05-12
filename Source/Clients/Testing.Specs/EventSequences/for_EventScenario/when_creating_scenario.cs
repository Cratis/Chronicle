// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

public class when_creating_scenario : Specification
{
    EventScenario _scenario;
    Exception _error;

    async Task Because() => _error = await Catch.Exception(() =>
    {
        _scenario = new EventScenario();

        return Task.CompletedTask;
    });

    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_have_event_log() => _scenario.EventLog.ShouldNotBeNull();
    [Fact] void should_have_event_sequence() => _scenario.EventSequence.ShouldNotBeNull();
    [Fact] void should_have_given_builder() => _scenario.Given.ShouldNotBeNull();
}
