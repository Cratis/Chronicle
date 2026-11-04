// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_borrowing_an_event_store.given;
using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_projecting_children_with_a_constant_parent_key.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_borrowing_an_event_store;

public class and_projecting_children_under_a_constant_key : a_borrowed_store
{
    ReadModelScenario<CountsReadModel> _borrowed;
    ReadModelScenario<CountsReadModel> _fresh;
    EventSourceId _staysOpen;
    EventSourceId _getsClosed;

    void Establish()
    {
        _borrowed = Borrowed<CountsReadModel>();
        _fresh = Fresh<CountsReadModel>();
        _staysOpen = new(Guid.NewGuid());
        _getsClosed = new(Guid.NewGuid());
    }

    async Task Because()
    {
        foreach (var scenario in new[] { _borrowed, _fresh })
        {
            await scenario.Given.ForEventSource(_staysOpen).Events(new CountedThingRegistered("stays open", true));
            await scenario.Given.ForEventSource(_getsClosed).Events(new CountedThingRegistered("gets closed", true));
            await scenario.Given.ForEventSource(_getsClosed).Events(new CountedThingClosed());
        }
    }

    [Fact] void should_count_every_registration() => _borrowed.Instance!.Registrations.ShouldEqual(2);
    [Fact] void should_count_only_the_thing_still_open() => _borrowed.Instance!.Open.ShouldEqual(1);
    [Fact] void should_match_a_fresh_scenario() => Json(_borrowed.Instances).ShouldEqual(Json(_fresh.Instances));
}
