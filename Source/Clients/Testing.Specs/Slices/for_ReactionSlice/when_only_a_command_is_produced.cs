// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.Reactors;

namespace Cratis.Chronicle.Testing.Slices.for_ReactionSlice;

public class when_only_a_command_is_produced : a_reaction_slice<VibeReminderReactor>
{
    Exception _error;

    void Establish() => _slice.GivenEvents(_source, new VibeStarted("host"));

    async Task Because()
    {
        var outcome = await _slice.Run();
        _error = Catch.Exception(() => outcome.ShouldHaveProducedEventsInOrder().ShouldHaveProducedEventsInAnyOrder().ShouldNotHaveProducedEvents()
            .Should(scenario => scenario.ShouldHaveProduced<SendReminder>(command => command.Host == "host")));
    }

    [Fact] void should_exclude_commands_from_event_assertions() => _error.ShouldBeNull();
    [Fact] void should_retain_existing_command_recording() => _scenario.Produced.Count.ShouldEqual(1);
}
