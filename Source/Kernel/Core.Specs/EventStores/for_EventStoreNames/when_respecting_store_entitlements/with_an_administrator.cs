// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Chronicle.EventStores.for_EventStoreNames.when_respecting_store_entitlements;

public class with_an_administrator : given.a_store_list_with_entitlements
{
    void Establish() => ((ClaimsIdentity)_caller.Identity).AddClaim(new("role", "operator", ClaimValueTypes.String, "https://authority.example"));

    async Task Because() => await ReadLists();

    [Fact] void should_list_every_store() => _snapshot.Select(store => store.Name).ShouldEqual(["first", "second", "System"]);
    [Fact] void should_observe_every_store() => _observed.ShouldEqual(_snapshot);
}
