// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;
using Grpc.Core;

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.with_real_ownership;

public class a_keep_alive_from_another_principal : given.a_service_with_real_ownership
{
    Exception _result;
    int _pingsBefore;

    async Task Establish() => await Open(AuthenticatedCalls.As("alice"));

    async Task Because()
    {
        _pingsBefore = Calls(nameof(IConnectedClients.OnClientPing));
        _result = await KeepAlive(AuthenticatedCalls.As("bob"));
    }

    [Fact] void should_be_refused() => ((RpcException)_result).StatusCode.ShouldEqual(StatusCode.PermissionDenied);
    [Fact] void should_not_ping_the_registry() => Calls(nameof(IConnectedClients.OnClientPing)).ShouldEqual(_pingsBefore);
    [Fact] void should_not_restore_the_client() => Calls(nameof(IConnectedClients.OnClientRestored)).ShouldEqual(0);
    [Fact] void should_leave_the_owner_in_place() => Owns("alice").ShouldBeTrue();
}
