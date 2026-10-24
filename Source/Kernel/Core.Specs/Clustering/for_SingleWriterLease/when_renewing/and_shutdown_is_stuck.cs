// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clustering;

namespace Cratis.Chronicle.SingleWriter.for_SingleWriterLease.when_renewing;

public class and_shutdown_is_stuck : given.a_keeper
{
    void Establish()
    {
        Storage.Renew(Arg.Any<SingleWriterLeaseOwner>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()).Returns(new TaskCompletionSource<bool>().Task);
    }

    async Task Because()
    {
        await Observer.OnStart(CancellationToken.None);
        Time.Advance(TimeSpan.FromSeconds(29));
    }

    [Fact] void should_fence_before_expiry() => FailFast.Received(1).FailFast(Arg.Any<string>());
}
