// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario;

public class when_a_reactor_produces_a_command : Specification
{
    const string Host = "Ada";

    ReactorScenario<VibeReminderReactor> _scenario;
    readonly EventSourceId _vibeId = EventSourceId.New();

    void Establish() => _scenario = new ReactorScenario<VibeReminderReactor>();

    async Task Because() => await _scenario.Given.ForEventSource(_vibeId).Events(new VibeStarted(Host));

    [Fact] void should_produce_a_send_reminder_command() => _scenario.ShouldHaveProduced<SendReminder>(r => r.Host == Host);
    [Fact] void should_not_produce_unrelated_side_effects() => _scenario.ShouldNotHaveProduced<MemberActivityRecorded>();
}
