// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario;

public class when_a_constructor_dependency_is_not_registered : Specification
{
    ReactorScenario<VibeCancellationReactor> _scenario;
    EventSourceId _vibeId;
    Exception _exception;

    void Establish()
    {
        _vibeId = EventSourceId.New();
        _scenario = new ReactorScenario<VibeCancellationReactor>();

        // INotificationService is intentionally left unregistered so activation fails.
        _scenario.Given.ForEventSourceId(_vibeId).ReadModel(new VibeAttendees(_vibeId, "Ada"));
    }

    async Task Because() =>
        _exception = await Catch.Exception(() => _scenario.Given.ForEventSource(_vibeId).Events(new VibeCancelled()));

    [Fact] void should_fail_to_activate_the_reactor() => _exception.ShouldBeOfExactType<CannotActivateReactorForScenario>();
}
