// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.SingleWriter;

namespace Cratis.Chronicle.SingleWriter.for_SingleWriterLease.when_starting;

public class and_storage_cancels_an_acquisition_without_host_cancellation : given.a_keeper
{
    void Establish()
    {
        var calls = 0;
        Storage.TryAcquire(Arg.Any<SingleWriterLeaseClaim>(), Arg.Any<CancellationToken>()).Returns(_ =>
            ++calls == 1 ? Task.FromException<SingleWriterLeaseAttempt>(new OperationCanceledException("storage timeout")) : Task.FromResult(new SingleWriterLeaseAttempt(true, null)));
    }

    async Task Because()
    {
        var start = Observer.OnStart(CancellationToken.None);
        Time.Advance(TimeSpan.FromSeconds(2));
        await start;
    }

    [Fact] void should_retry_the_acquisition() => Storage.Received(2).TryAcquire(Arg.Any<SingleWriterLeaseClaim>(), Arg.Any<CancellationToken>());
    [Fact] void should_hold_the_lease() => Keeper.IsHeld.ShouldBeTrue();
}
