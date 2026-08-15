// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_clearing_a_scalar_member;

/// <summary>
/// A clear is a value written at a point in the stream, not a terminal state for the member. A later event writes
/// over it exactly as it would over any other value.
/// </summary>
public class and_the_note_is_written_again_after_the_clear : Specification
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
            .Events(new ProjectNoted("Check the wiring"), new ProjectNoteCleared(), new ProjectNoted("Wiring checked"));

    [Fact] void should_hold_the_later_note() => _scenario.Instance!.Note.ShouldEqual("Wiring checked");
}
