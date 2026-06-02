// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Observers.for_querying_observers_with_observer_id;

public class when_checking_if_observer_exists_by_id : given.an_event_store_db_context
{
    static readonly ObserverId _existing = new("existing-observer");
    static readonly ObserverId _missing = new("missing-observer");
    bool _existingFound;
    bool _missingFound;

    void Establish()
    {
        context.Observers.Add(new ObserverDefinition
        {
            Id = _existing,
            EventTypes = []
        });
        context.SaveChanges();
    }

    async Task Because()
    {
        _existingFound = await context.Observers.AnyAsync(o => o.Id == _existing);
        _missingFound = await context.Observers.AnyAsync(o => o.Id == _missing);
    }

    [Fact] void should_find_existing_observer() => _existingFound.ShouldBeTrue();
    [Fact] void should_not_find_missing_observer() => _missingFound.ShouldBeFalse();
}
