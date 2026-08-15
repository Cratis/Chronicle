// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_clearing_with_to_value_null;

/// <summary>
/// The older spelling keeps working. Before the scalar clear existed this wrote the four-character string "null"
/// into the member, so a spec that only checked "not the previous value" would have passed on the bug - the note
/// is asserted to be null, and the sibling constant is asserted to be untouched.
/// </summary>
public class and_the_clearing_event_has_occurred : Specification
{
    ReadModelScenario<FluentArchivedNote> _scenario;
    EventSourceId _id;

    void Establish()
    {
        _id = EventSourceId.New();
        _scenario = new ReadModelScenario<FluentArchivedNote>();
    }

    async Task Because() =>
        await _scenario.Given
            .ForEventSource(_id)
            .Events(new ProjectNoted("Check the wiring"), new ProjectNoteCleared());

    [Fact] void should_clear_the_note() => _scenario.Instance!.Note.ShouldBeNull();
    [Fact] void should_not_write_the_text_of_the_keyword() => _scenario.Instance!.Note.ShouldNotEqual("null");
    [Fact] void should_leave_a_real_constant_alone() => _scenario.Instance!.Status.ShouldEqual("archived");
}
