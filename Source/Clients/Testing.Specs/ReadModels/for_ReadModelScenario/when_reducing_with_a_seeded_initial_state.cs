// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario;

/// <summary>
/// Verifies that <see cref="ReadModelScenario{TReadModel}"/> seeds the reducer with the provided
/// initial state, so a reducer reduces incoming events on top of it rather than starting from
/// <see langword="null"/>.
/// </summary>
public class when_reducing_with_a_seeded_initial_state : Specification
{
    ReadModelScenario<Tally> _scenario;
    Guid _idGuid;
    EventSourceId _id;

    void Establish()
    {
        _idGuid = Guid.NewGuid();
        _id = new EventSourceId(_idGuid);
        _scenario = new ReadModelScenario<Tally>(new Tally(_idGuid, 100));
    }

    async Task Because() =>
        await _scenario.Given.ForEventSource(_id).Events(new Tallied());

    [Fact] void should_apply_the_event_on_top_of_the_seeded_state() => _scenario.Instance!.Count.ShouldEqual(101);
}
