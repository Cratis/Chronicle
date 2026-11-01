// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverStateStorage.when_handing_over;

public class without_an_existing_observer : given.an_observer_state_storage
{
    bool _accepted;
    int _count;

    async Task Because()
    {
        _accepted = await _storage.HandOver("observer", null, ObserverSequenceCheckpoint.Fresh("observer", "B"));
        await using var context = CreateContext();
        _count = await context.Observers.CountAsync();
    }

    [Fact] void should_not_accept_a_missing_observer() => _accepted.ShouldBeFalse();
    [Fact] void should_not_create_an_observer() => _count.ShouldEqual(0);
}
