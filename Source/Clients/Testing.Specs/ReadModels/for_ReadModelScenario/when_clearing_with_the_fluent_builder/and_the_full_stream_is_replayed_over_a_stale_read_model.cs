// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_clearing_with_the_fluent_builder;

/// <summary>
/// A fluent clear has to survive a rebuild for the same reason the attribute form does: it is a value written into
/// the changeset, not the absence of a mapping. Replaying the whole stream onto a document that already carries a
/// note has to end with the note gone.
/// </summary>
public class and_the_full_stream_is_replayed_over_a_stale_read_model : Specification
{
    ReadModelScenario<FluentProjectNotes> _scenario;
    EventSourceId _id;

    void Establish()
    {
        _id = EventSourceId.New();
        _scenario = new ReadModelScenario<FluentProjectNotes>(
            new FluentProjectNotes(Guid.Parse(_id), "Stale from before the replay", null, []));
    }

    async Task Because() =>
        await _scenario.Given
            .ForEventSource(_id)
            .Events(new ProjectNoted("Check the wiring"), new ProjectNoteCleared());

    [Fact] void should_clear_the_root_note() => _scenario.Instance!.Note.ShouldBeNull();
}
