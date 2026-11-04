// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Clients;
using Grpc.Core;

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.when_keeping_alive;

public class and_another_principal_owns_the_id : given.a_caller_bound_connection
{
    async Task Because()
    {
        _owners.Principal = _caller;
        try
        {
            _error = await Catch.Exception(() => _service.ConnectionKeepAlive(new ConnectionKeepAlive { ConnectionId = "connection" }));
        }
        finally
        {
            _owners.Principal = null;
        }
    }

    [Fact] void should_deny_the_keep_alive() => ((RpcException)_error).StatusCode.ShouldEqual(StatusCode.PermissionDenied);
}
