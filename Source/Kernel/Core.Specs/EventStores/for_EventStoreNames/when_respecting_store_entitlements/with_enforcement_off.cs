// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.EventStores.for_EventStoreNames.when_respecting_store_entitlements;

public class with_enforcement_off : given.a_store_list_with_entitlements
{
    void Establish() => Configure(new Authentication { EnforceStoreEntitlements = false });

    async Task Because() => await ReadLists();

    [Fact] void should_leave_the_list_unchanged() => _snapshot.Select(store => store.Name).ShouldEqual(["first", "second", "System"]);
    [Fact] void should_leave_the_observable_unchanged() => _observed.ShouldEqual(_snapshot);
}
