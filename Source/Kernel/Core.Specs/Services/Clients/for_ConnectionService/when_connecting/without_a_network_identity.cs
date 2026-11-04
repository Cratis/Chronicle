// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Contracts.Clients;
using Grpc.Core;
using Microsoft.AspNetCore.Http;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.when_connecting;

public class without_a_network_identity : given.a_gated_connection_service
{
    Exception _error;

    async Task Because()
    {
        var http = Substitute.For<HttpContext>();
        http.User.Returns((ClaimsPrincipal)null!);
        var server = Substitute.ForPartsOf<ServerCallContext>();
        server.UserState["__HttpContext"] = http;
        var rejected = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var subscription = _service.Connect(new ConnectRequest { ConnectionId = "connection" }, new CallContext(this, server))
            .Subscribe(_ => { }, error => rejected.TrySetResult(error));
        _error = await rejected.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_deny_the_connection() => ((RpcException)_error).StatusCode.ShouldEqual(StatusCode.PermissionDenied);
    [Fact] void should_not_claim_it_as_in_process() => _ownership.GetClaim("connection").ShouldBeNull();
    [Fact] void should_not_touch_the_registry() => _clients.ReceivedCalls().ShouldBeEmpty();
}
