// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.ReadModels.for_ReadModels.when_watching.across_scopes;

public class and_a_change_is_produced_only_in_another_event_store : given.a_composed_watch_topology
{
    Watch _watchS;
    Watch _watchT;

    async Task Because()
    {
        _watchS = await StartWatch(StoreS, NamespaceA);
        _watchT = await StartWatch(StoreT, NamespaceA);
        var producer = await given.ProjectionProducer.Create(_router, ObserverId, StoreT, NamespaceA);

        await producer.Produce("value-t");
    }

    [Fact] void should_deliver_to_the_watch_of_that_event_store() => Values(_watchT).ShouldEqual(["value-t"]);
    [Fact] void should_not_deliver_to_the_watch_of_the_other_event_store() => Values(_watchS).ShouldBeEmpty();
    [Fact] void should_release_only_for_that_event_store() => ReleaseCalls().Distinct().ShouldContainOnly([(StoreT, NamespaceA)]);
}
