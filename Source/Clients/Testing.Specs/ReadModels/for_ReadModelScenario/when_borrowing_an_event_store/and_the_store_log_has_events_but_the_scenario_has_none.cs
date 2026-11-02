// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_borrowing_an_event_store.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_borrowing_an_event_store;

public class and_the_store_log_has_events_but_the_scenario_has_none : a_borrowed_store
{
    ReadModelScenario<Tally> _borrowed;
    EventSourceId _id;
    Tally _read;

    void Establish()
    {
        _id = new(Guid.NewGuid());
        _borrowed = Borrowed<Tally>();
    }

    async Task Because()
    {
        await _store.EventLog.Append(_id, new Tallied());
        _read = await _borrowed.ReadModels.GetInstanceById<Tally>(_id);
    }

    [Fact] void should_have_no_instance() => _borrowed.Instance.ShouldBeNull();
    [Fact] void should_have_no_instances() => _borrowed.Instances.ShouldBeEmpty();
    [Fact] void should_not_read_the_store_log_by_key() => _read.ShouldBeNull();
}
