// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_clearing_a_scalar_member;

/// <summary>
/// The control for the clear. Without it a null note would prove nothing - a projection that never wrote the note
/// at all would pass the clearing spec just as well.
/// </summary>
public class and_the_clearing_event_has_not_occurred : Specification
{
    ReadModelScenario<ProjectNotes> _scenario;
    EventSourceId _id;

    void Establish()
    {
        _id = EventSourceId.New();
        _scenario = new ReadModelScenario<ProjectNotes>();
    }

    async Task Because() =>
        await _scenario.Given
            .ForEventSource(_id)
            .Events(new ProjectNoted("Check the wiring"));

    [Fact] void should_keep_the_note() => _scenario.Instance!.Note.ShouldEqual("Check the wiring");
}
