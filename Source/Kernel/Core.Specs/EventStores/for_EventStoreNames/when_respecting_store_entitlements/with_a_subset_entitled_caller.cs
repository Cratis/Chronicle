// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventStores.for_EventStoreNames.when_respecting_store_entitlements;

public class with_a_subset_entitled_caller : given.a_store_list_with_entitlements
{
    async Task Because() => await ReadLists();

    [Fact] void should_list_only_the_granted_store() => _snapshot.ShouldEqual([new EventStoreNames("first", "first")]);
    [Fact] void should_observe_only_the_granted_store() => _observed.ShouldEqual(_snapshot);
}
