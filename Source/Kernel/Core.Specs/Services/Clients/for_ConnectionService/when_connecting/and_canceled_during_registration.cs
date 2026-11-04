// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.when_connecting;

public class and_canceled_during_registration : given.a_gated_connection_service
{
    Exception _error;

    async Task Because()
    {
        var registering = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var finishRegistration = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _clients.OnClientConnected(Arg.Any<ConnectionId>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(_ =>
            {
                registering.TrySetResult();
                return finishRegistration.Task;
            });
        var cancellation = new CancellationTokenSource();
        var stream = Start(cancellation);
        try
        {
            await registering.Task.WaitAsync(TimeSpan.FromSeconds(5));
            cancellation.Cancel();
        }
        finally
        {
            finishRegistration.TrySetResult();
        }

        _error = (await stream.Completed.WaitAsync(TimeSpan.FromSeconds(5)))!;
    }

    [Fact] void should_complete_without_error() => _error.ShouldBeNull();
    [Fact] void should_release_the_claim_after_cleanup() => _ownership.GetClaim("connection").ShouldBeNull();
    [Fact] Task should_disconnect_despite_the_canceled_request() => _clients.Received(1).OnClientDisconnected("connection", "Client disconnected");
}
