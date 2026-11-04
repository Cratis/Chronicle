// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.when_connecting;

public class and_canceled_while_waiting_for_the_gate : given.a_gated_connection_service
{
    Exception _error;

    async Task Because()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _ownership.Entering = () => entered.TrySetResult();
        var cancellation = new CancellationTokenSource();
        var gate = await _ownership.Enter("connection");
        var stream = Start(cancellation);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            cancellation.Cancel();
        }
        finally
        {
            gate.Dispose();
        }

        _error = (await stream.Completed.WaitAsync(TimeSpan.FromSeconds(5)))!;
    }

    [Fact] void should_complete_without_error() => _error.ShouldBeNull();
    [Fact] void should_not_claim_the_connection() => _ownership.GetClaim("connection").ShouldBeNull();
    [Fact] void should_not_touch_the_registry() => _clients.ReceivedCalls().ShouldBeEmpty();
}
