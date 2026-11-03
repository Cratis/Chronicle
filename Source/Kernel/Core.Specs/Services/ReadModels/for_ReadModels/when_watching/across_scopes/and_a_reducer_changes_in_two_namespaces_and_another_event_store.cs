// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.ReadModels.for_ReadModels.when_watching.across_scopes;

public class and_a_reducer_changes_in_two_namespaces_and_another_event_store : given.a_composed_watch_topology
{
    Watch _watchA;
    Watch _watchB;
    Watch _watchOtherStore;

    void Establish() => MakeReducerBacked();

    async Task Because()
    {
        _watchA = await StartWatch(StoreS, NamespaceA);
        _watchB = await StartWatch(StoreS, NamespaceB);
        _watchOtherStore = await StartWatch(StoreT, NamespaceA);
        var producerA = await given.ReducerProducer.Create(_router, ObserverId, StoreS, NamespaceA);
        var producerB = await given.ReducerProducer.Create(_router, ObserverId, StoreS, NamespaceB);

        await producerA.Produce("value-a");
        await producerB.Produce("value-b");
    }

    [Fact] void should_deliver_the_change_to_its_own_namespace() => Values(_watchA).ShouldEqual(["value-a"]);
    [Fact] void should_deliver_the_other_namespace_its_own_change() => Values(_watchB).ShouldEqual(["value-b"]);
    [Fact] void should_not_deliver_to_the_same_namespace_in_another_event_store() => Values(_watchOtherStore).ShouldBeEmpty();
    [Fact] void should_release_only_own_deliveries() => ReleaseCalls().Distinct().ShouldContainOnly([(StoreS, NamespaceA), (StoreS, NamespaceB)]);
    [Fact] void should_release_exactly_once_per_delivery() => ReleaseCalls().Length.ShouldEqual(2);
}
