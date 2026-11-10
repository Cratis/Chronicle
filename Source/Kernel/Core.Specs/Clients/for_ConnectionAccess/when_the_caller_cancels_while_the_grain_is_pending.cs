// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ConnectionAccess;

public class when_the_caller_cancels_while_the_grain_is_pending : given.a_connection_check
{
    Exception _error;

    void Establish() => _reservation.IsHeldBy(Alice).Returns(new TaskCompletionSource<bool>().Task);
    async Task Because()
    {
        using var cancellation = new CancellationTokenSource();
        var pending = _ownership.Verify(_factory, Id, Alice, Timeout, cancellationToken: cancellation.Token);
        await cancellation.CancelAsync();
        _error = await Catch.Exception(async () => await pending);
    }

    [Fact] void should_propagate_cancellation() => (_error is OperationCanceledException).ShouldBeTrue();
}
