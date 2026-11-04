// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ConnectionOwnership;

public class when_a_gate_wait_is_canceled : Specification
{
    readonly ConnectionOwnership _ownership = new();
    bool _canceled;
    bool _nextWaiterAcquired;

    async Task Because()
    {
        using var cancellation = new CancellationTokenSource();
        var first = await _ownership.Enter("connection");
        var waiting = _ownership.Enter("connection", cancellation.Token);
        cancellation.Cancel();
        try
        {
            using var unexpected = await waiting;
        }
        catch (OperationCanceledException)
        {
            _canceled = true;
        }
        finally
        {
            first.Dispose();
        }

        using var next = await _ownership.Enter("connection").AsTask().WaitAsync(TimeSpan.FromSeconds(5));
        _nextWaiterAcquired = true;
    }

    [Fact] void should_cancel_only_the_waiter() => _canceled.ShouldBeTrue();
    [Fact] void should_not_lose_the_gate_permit() => _nextWaiterAcquired.ShouldBeTrue();
}
