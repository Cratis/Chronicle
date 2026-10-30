// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.SingleWriter;

namespace Cratis.Chronicle.SingleWriter.for_SingleWriterLease.when_starting;

public class and_the_host_cancels_a_pending_acquisition : given.a_keeper
{
    Exception _error;

    void Establish() => Storage.TryAcquire(Arg.Any<SingleWriterLeaseClaim>(), Arg.Any<CancellationToken>()).Returns(new TaskCompletionSource<SingleWriterLeaseAttempt>().Task);

    async Task Because()
    {
        using var cancellation = new CancellationTokenSource();
        var start = Observer.OnStart(cancellation.Token);
        await cancellation.CancelAsync();
        _error = await Catch.Exception(() => start);
    }

    [Fact] void should_honor_host_cancellation() => (_error is OperationCanceledException).ShouldBeTrue();
    [Fact] void should_not_become_ready() => Keeper.IsHeld.ShouldBeFalse();
}
