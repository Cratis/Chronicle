// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;
using Grpc.Core;

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.with_real_ownership;

public class a_connection_opened_by_a_call_without_a_request : given.a_service_with_real_ownership
{
    Exception _result;

    async Task Because() => _result = await Open(AuthenticatedCalls.WithoutRequest());

    [Fact] void should_be_refused() => ((RpcException)_result).StatusCode.ShouldEqual(StatusCode.PermissionDenied);
    [Fact] void should_not_register_the_client() => Calls(nameof(IConnectedClients.OnClientConnected)).ShouldEqual(0);
}
