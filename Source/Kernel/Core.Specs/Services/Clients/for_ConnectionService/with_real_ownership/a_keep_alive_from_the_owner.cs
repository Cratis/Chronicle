// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.with_real_ownership;

public class a_keep_alive_from_the_owner : given.a_service_with_real_ownership
{
    Exception _result;

    async Task Establish() => await Open(AuthenticatedCalls.As("alice"));

    async Task Because() => _result = await KeepAlive(AuthenticatedCalls.As("alice"));

    [Fact] void should_be_accepted() => _result.ShouldBeNull();
}
