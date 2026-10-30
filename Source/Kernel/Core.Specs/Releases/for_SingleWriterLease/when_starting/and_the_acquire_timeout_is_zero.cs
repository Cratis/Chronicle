// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.SingleWriter;

namespace Cratis.Chronicle.SingleWriter.for_SingleWriterLease.when_starting;

public class and_the_acquire_timeout_is_zero : given.a_keeper
{
    Exception _error;
    void Establish()
    {
        Options.Clustering.SingleWriterLease.AcquireTimeout = TimeSpan.Zero;
        CreateKeeper();
        Storage.TryAcquire(Arg.Any<SingleWriterLeaseClaim>(), Arg.Any<CancellationToken>()).Returns(new SingleWriterLeaseAttempt(false, null));
    }

    async Task Because()
    {
        _error = await Catch.Exception(() => Observer.OnStart(CancellationToken.None));
    }

    [Fact] void should_attempt_once() => Storage.Received(1).TryAcquire(Arg.Any<SingleWriterLeaseClaim>(), Arg.Any<CancellationToken>());
    [Fact] void should_fail_without_waiting() => _error.ShouldBeOfExactType<SingleWriterLeaseNotAcquired>();
}
