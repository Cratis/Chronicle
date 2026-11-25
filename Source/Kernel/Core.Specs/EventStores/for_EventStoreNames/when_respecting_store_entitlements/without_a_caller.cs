// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.EventStores.for_EventStoreNames.when_respecting_store_entitlements;

public class without_a_caller : given.a_store_list_with_entitlements
{
    void Establish() => _services.GetRequiredService<IHttpContextAccessor>().HttpContext.User = new ClaimsPrincipal();

    async Task Because() => await ReadLists();

    [Fact] void should_list_no_stores() => _snapshot.ShouldBeEmpty();
    [Fact] void should_observe_no_stores() => _observed.ShouldBeEmpty();
}
