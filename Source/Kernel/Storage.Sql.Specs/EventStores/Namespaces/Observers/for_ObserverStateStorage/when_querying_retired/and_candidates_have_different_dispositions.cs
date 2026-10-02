// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverStateStorage.when_querying_retired;

public class and_candidates_have_different_dispositions : given.an_observer_state_storage
{
    IEnumerable<ObserverId> _result;

    async Task Establish()
    {
        await using var context = CreateContext();
        context.Observers.AddRange(
            new ObserverState { Id = "retired", AlertDisposition = AlertDisposition.Retired },
            new ObserverState { Id = "active", AlertDisposition = AlertDisposition.Active },
            new ObserverState { Id = "unsubmitted", AlertDisposition = AlertDisposition.Retired });
        await context.SaveChangesAsync();
    }

    async Task Because() => _result = await _storage.GetRetired(["retired", "active", "missing", "retired"]);

    [Fact] void should_only_return_the_submitted_retired_observer() => _result.ShouldContainOnly((ObserverId)"retired");
}
