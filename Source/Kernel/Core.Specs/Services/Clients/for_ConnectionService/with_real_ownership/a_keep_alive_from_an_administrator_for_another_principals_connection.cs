// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;
using Grpc.Core;

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.with_real_ownership;

public class a_keep_alive_from_an_administrator_for_another_principals_connection : given.a_service_with_real_ownership
{
    Exception _result;
    Exception _reopened;

    async Task Establish() => await Open(AuthenticatedCalls.As("alice"));

    async Task Because()
    {
        _result = await KeepAlive(AuthenticatedCalls.As("admin", administrator: true));
        _reopened = await Open(AuthenticatedCalls.As("admin", administrator: true));
    }

    [Fact] void should_be_refused() => ((RpcException)_result).StatusCode.ShouldEqual(StatusCode.PermissionDenied);
    [Fact] void should_not_restore_the_client() => Calls(nameof(IConnectedClients.OnClientRestored)).ShouldEqual(0);
    [Fact] void should_not_let_the_administrator_open_the_same_id() => _reopened.ShouldNotBeNull();
    [Fact] void should_leave_the_owner_in_place() => Owns("alice").ShouldBeTrue();
}
