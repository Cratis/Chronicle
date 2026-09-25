// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario;

public class when_a_reactor_produces_a_command_synchronously : Specification
{
    ReactorScenario<SynchronousCommandReactor> _scenario;

    void Establish() => _scenario = new ReactorScenario<SynchronousCommandReactor>();

    async Task Because() => await _scenario.Given.ForEventSource(EventSourceId.New()).Events(new VibeStarted("Ada"));

    [Fact] void should_record_the_command() => _scenario.ShouldHaveProduced<SendReminder>();

    public class SynchronousCommandReactor : IReactor
    {
        public SendReminder Handle(VibeStarted @event) => new(@event.Host);
    }
}
