// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Observers.for_querying_observers_with_observer_id;

public class when_querying_observer_by_id : given.an_event_store_db_context
{
    static readonly ObserverId _targetId = new("target-observer");
    static readonly ObserverId _otherId = new("other-observer");
    ObserverDefinition? _result;

    void Establish()
    {
        context.Observers.AddRange(
            new ObserverDefinition { Id = _targetId, EventTypes = [] },
            new ObserverDefinition { Id = _otherId, EventTypes = [] });
        context.SaveChanges();
    }

    async Task Because() =>
        _result = await context.Observers.FirstOrDefaultAsync(o => o.Id == _targetId);

    [Fact] void should_return_the_matching_observer() => _result.ShouldNotBeNull();
    [Fact] void should_return_the_correct_observer() => _result!.Id.ShouldEqual(_targetId);
}
