// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.when_redelivering;

public class with_a_returned_command : Specification
{
    ReactorScenario<VibeReminderReactor> _scenario;
    RecordedOccurrence _occurrence;

    async Task Establish()
    {
        _scenario = new();
        _occurrence = await _scenario.Given.ForEventSource("vibe").History(new VibeStarted("Ada"));
    }

    async Task Because()
    {
        await _scenario.Redeliver(_occurrence);
        await _scenario.Redeliver(_occurrence);
    }

    void Destroy() => _scenario.Dispose();

    [Fact] void should_record_returned_commands() => _scenario.ShouldHaveProduced<SendReminder>(command => command.Host == "Ada");
    [Fact] void should_accumulate_commands_across_deliveries() => _scenario.Produced.Count.ShouldEqual(2);
    [Fact] void should_not_record_commands_as_events() => _scenario.ProducedEvents.ShouldBeEmpty();
}
