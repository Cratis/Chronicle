// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_clearing_a_scalar_member;

/// <summary>
/// The behavior #3641 reported missing: a scalar member declared with <c>[ClearWith]</c> is actually written back
/// to no value when the clearing event is observed, rather than keeping whatever it last held.
/// </summary>
public class and_the_clearing_event_has_occurred : Specification
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
            .Events(new ProjectNoted("Check the wiring"), new ProjectNoteCleared());

    [Fact] void should_have_materialized_the_read_model() => _scenario.Instance.ShouldNotBeNull();
    [Fact] void should_clear_the_note() => _scenario.Instance!.Note.ShouldBeNull();
}
