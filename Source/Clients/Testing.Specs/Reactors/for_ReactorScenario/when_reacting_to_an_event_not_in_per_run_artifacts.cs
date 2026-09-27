// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario;

public class when_reacting_to_an_event_not_in_per_run_artifacts : Specification
{
    ReactorScenario<ReservationReactor> _scenario;
    Exception _exception;

    void Establish()
    {
        var artifacts = Substitute.For<IClientArtifactsProvider>();
        artifacts.EventTypes.Returns([typeof(VibeStarted)]);
        _scenario = new ReactorScenario<ReservationReactor>(new Defaults(artifacts));
    }

    async Task Because() => _exception = await Catch.Exception(async () =>
        await _scenario.Given.ForEventSource(EventSourceId.New()).Events(new ReservationMade("member")));

    [Fact] void should_reject_the_unregistered_event_type() => _exception.ShouldBeOfExactType<TypeIsNotAnEventType>();
}
