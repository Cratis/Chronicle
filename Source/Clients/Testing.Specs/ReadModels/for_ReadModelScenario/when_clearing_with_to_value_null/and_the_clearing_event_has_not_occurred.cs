// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_clearing_with_to_value_null;

/// <summary>
/// The control: the note is really written, and a ToValue carrying a real constant really sets it.
/// </summary>
public class and_the_clearing_event_has_not_occurred : Specification
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
            .Events(new ProjectNoted("Check the wiring"));

    [Fact] void should_hold_the_note() => _scenario.Instance!.Note.ShouldEqual("Check the wiring");
    [Fact] void should_hold_the_constant() => _scenario.Instance!.Status.ShouldEqual("open");
}
