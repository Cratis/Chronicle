// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Clients;
using Grpc.Core;

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.when_connecting;

public class and_another_principal_owns_the_id : given.a_caller_bound_connection
{
    async Task Because()
    {
        var rejected = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var subscription = _service.Connect(new ConnectRequest { ConnectionId = "connection" }, _context).Subscribe(_ => { }, error => rejected.TrySetResult(error));
        _error = await rejected.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_deny_the_connection() => ((RpcException)_error).StatusCode.ShouldEqual(StatusCode.PermissionDenied);
}
