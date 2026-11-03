// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverStateStorage.when_observing_all;

public class and_nothing_changes : given.an_observing_observer_state_storage
{
    async Task Because()
    {
        await Subscribe();
        await WaitForCompletedScopes(10);
    }

    [Fact] void should_emit_only_the_initial_snapshot() => _emissions.Count.ShouldEqual(1);
}
