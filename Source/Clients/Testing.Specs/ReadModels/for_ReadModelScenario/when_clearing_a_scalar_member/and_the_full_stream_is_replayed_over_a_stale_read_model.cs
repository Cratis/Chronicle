// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_clearing_a_scalar_member;

/// <summary>
/// The half of #3641 that made the defect permanent: the stale value survived a full replay. It survived because
/// no mapping was ever emitted for the member, so replaying the stream wrote nothing over what was already stored.
/// Replaying the whole stream onto a document that already carries a note has to end with the note gone.
/// </summary>
public class and_the_full_stream_is_replayed_over_a_stale_read_model : Specification
{
    ReadModelScenario<ProjectNotes> _scenario;
    EventSourceId _id;

    void Establish()
    {
        _id = EventSourceId.New();
        _scenario = new ReadModelScenario<ProjectNotes>(new ProjectNotes(Guid.Parse(_id), "Stale from before the replay"));
    }

    async Task Because() =>
        await _scenario.Given
            .ForEventSource(_id)
            .Events(new ProjectNoted("Check the wiring"), new ProjectNoteCleared());

    [Fact] void should_clear_the_note() => _scenario.Instance!.Note.ShouldBeNull();
}
