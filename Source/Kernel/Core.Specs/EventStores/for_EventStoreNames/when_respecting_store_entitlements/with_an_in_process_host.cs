// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.EventStores.for_EventStoreNames.when_respecting_store_entitlements;

public class with_an_in_process_host : given.a_store_list_with_entitlements
{
    void Establish()
    {
        Configure(_authentication, inProcess: true);
        _services.GetRequiredService<IHttpContextAccessor>().HttpContext = null;
        _cache.IsFresh.Returns(false);
    }

    async Task Because() => await ReadLists();

    [Fact] void should_list_every_store() => _snapshot.Select(store => store.Name).ShouldEqual(["first", "second", "System"]);
    [Fact] void should_observe_every_store() => _observed.ShouldEqual(_snapshot);
}
