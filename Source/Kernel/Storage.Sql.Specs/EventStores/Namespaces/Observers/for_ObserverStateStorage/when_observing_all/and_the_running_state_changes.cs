// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverStateStorage.when_observing_all;

public class and_the_running_state_changes : given.an_observing_observer_state_storage
{
    async Task Because()
    {
        await Subscribe();
        await Update();
        await WaitForEmission();
    }

    async Task Update()
    {
        await using var scope = await _database.Namespace(_eventStore, _namespace);
        await scope.DbContext.Observers
            .Where(observer => observer.Id == _observerId)
            .ExecuteUpdateAsync(set => set.SetProperty(observer => observer.RunningState, ObserverRunningState.Suspended));
    }

    [Fact] void should_emit_a_second_snapshot() => _emissions.Count.ShouldEqual(2);
    [Fact] void should_emit_the_new_running_state() => _emissions.Last().Single().RunningState.ShouldEqual(ObserverRunningState.Suspended);
}
