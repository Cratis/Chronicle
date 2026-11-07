// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;
using Grpc.Core;

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.with_real_ownership;

public class a_connection_opened_for_an_id_another_principal_holds : given.a_service_with_real_ownership
{
    Exception _result;

    async Task Establish() => await Open(AuthenticatedCalls.As("alice"));

    async Task Because() => _result = await Open(AuthenticatedCalls.As("bob"));

    [Fact] void should_be_refused() => ((RpcException)_result).StatusCode.ShouldEqual(StatusCode.PermissionDenied);
    [Fact] void should_register_only_the_first_client() => Calls(nameof(IConnectedClients.OnClientConnected)).ShouldEqual(1);
    [Fact] void should_leave_the_owner_in_place() => Owns("alice").ShouldBeTrue();
    [Fact] void should_not_make_the_other_caller_an_owner() => Owns("bob").ShouldBeFalse();
}
