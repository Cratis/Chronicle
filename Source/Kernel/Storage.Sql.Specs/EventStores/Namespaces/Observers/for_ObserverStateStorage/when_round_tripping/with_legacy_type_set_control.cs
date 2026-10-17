// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

using KernelObserverState = Cratis.Chronicle.Storage.Observation.ObserverState;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverStateStorage.when_round_tripping;

public class with_legacy_type_set_control : given.an_observer_state_storage
{
    KernelObserverState _saved;

    async Task Establish()
    {
        await _storage.Save(new() { Identifier = "observer" });
        await using var context = CreateContext();
        const string control = """
            {"Revision":7,"Contract":{"SubscriberType":"reactor-subscriber","Filters":{"Tags":[],"EventSourceType":"","EventStreamType":""}},"Members":{"event-a":{"Incarnation":3,"JoinFence":42}},"Registrations":[]}
            """;
        await context.Database.ExecuteSqlInterpolatedAsync($"UPDATE Observers SET TypeSetControl = {control} WHERE Id = 'observer'");
    }

    async Task Because() => _saved = await _storage.Get("observer");

    [Fact] void should_keep_the_committed_revision() => _saved.TypeSetControl.Revision.Value.ShouldEqual(7);
    [Fact] void should_keep_the_existing_member() => _saved.TypeSetControl.Members[new("event-a")].Incarnation.Value.ShouldEqual(3);
    [Fact] void should_not_invent_retirement_results() => _saved.TypeSetControl.RetirementResults.ShouldBeNull();
    [Fact] void should_not_invent_a_pending_change() => _saved.TypeSetControl.PendingChange.ShouldBeNull();
}
