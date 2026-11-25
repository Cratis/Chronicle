// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Security;

namespace Cratis.Chronicle.EventStores.for_EventStoreNames.when_respecting_store_entitlements;

public class with_an_entitled_caller : given.a_store_list_with_entitlements
{
    void Establish() => _cache.Snapshot.Returns(new StoreEntitlementsSnapshot(new(1,
        [new(PrincipalKey.From(_caller, _authentication), null, null, StoreAccess.Read, EventStoreAccessGrantSource.Command)])));

    async Task Because() => await ReadLists();

    [Fact] void should_list_all_granted_stores() => _snapshot.Select(store => store.Name).ShouldEqual(["first", "second"]);
    [Fact] void should_observe_all_granted_stores() => _observed.ShouldEqual(_snapshot);
}
