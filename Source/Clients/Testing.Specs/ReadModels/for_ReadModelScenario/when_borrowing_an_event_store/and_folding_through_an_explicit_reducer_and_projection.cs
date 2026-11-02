// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_borrowing_an_event_store.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_borrowing_an_event_store;

public class and_folding_through_an_explicit_reducer_and_projection : a_borrowed_store
{
    ReadModelScenario<Tally> _borrowedTally;
    ReadModelScenario<FluentContactSheet> _borrowedSheet;
    ReadModelScenario<FluentContactSheet> _freshSheet;
    SheetId _sheetId;
    ContactId _contact;

    void Establish()
    {
        _borrowedTally = Borrowed<Tally>();
        _borrowedSheet = Borrowed<FluentContactSheet>();
        _freshSheet = Fresh<FluentContactSheet>();
        _sheetId = SheetId.New();
        _contact = new(Guid.NewGuid());
    }

    async Task Because()
    {
        await _borrowedTally.Given.ForEventSource(new EventSourceId(Guid.NewGuid())).Events(new Tallied(), new Tallied(), new Tallied());
        foreach (var scenario in new[] { _borrowedSheet, _freshSheet })
        {
            await scenario.Given.ForEventSource(_sheetId).Events(new SheetStarted(2026), new ContactAssigned(_contact, "Ada"));
        }
    }

    [Fact] void should_reduce_through_the_reducer() => _borrowedTally.Instance!.Count.ShouldEqual(3);
    [Fact] void should_project_through_the_projection_class() => _borrowedSheet.Instance!.Contacts.Single().Name.ShouldEqual("Ada");
    [Fact] void should_match_a_fresh_scenario() => Json(_borrowedSheet.Instance).ShouldEqual(Json(_freshSheet.Instance));
}
