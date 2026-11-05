// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;
using Grpc.Core;

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.with_real_ownership;

public class a_keep_alive_replayed_after_the_owner_disconnected : given.a_service_with_real_ownership
{
    readonly CancellationTokenSource _cancellation = new();
    Exception _result;

    async Task Establish()
    {
        await Open(AuthenticatedCalls.As("alice", cancellationToken: _cancellation.Token));
        await _cancellation.CancelAsync();
        await ClaimReleased();
    }

    async Task Because() => _result = await KeepAlive(AuthenticatedCalls.As("bob"));

    [Fact] void should_be_refused() => ((RpcException)_result).StatusCode.ShouldEqual(StatusCode.PermissionDenied);
    [Fact] void should_not_ping_the_registry() => Calls(nameof(IConnectedClients.OnClientPing)).ShouldEqual(0);
    [Fact] void should_not_restore_the_client() => Calls(nameof(IConnectedClients.OnClientRestored)).ShouldEqual(0);
    [Fact] async Task should_not_let_the_other_caller_open_the_same_id() => (await Open(AuthenticatedCalls.As("bob"))).ShouldNotBeNull();
    [Fact] async Task should_not_let_the_released_id_be_pinged_by_the_previous_owner_either() => (await KeepAlive(AuthenticatedCalls.As("alice"))).ShouldNotBeNull();
}
