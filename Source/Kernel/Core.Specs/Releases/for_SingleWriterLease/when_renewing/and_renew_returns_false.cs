// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clustering;

namespace Cratis.Chronicle.SingleWriter.for_SingleWriterLease.when_renewing;

public class and_renew_returns_false : given.a_keeper
{
    void Establish()
    {
        Storage.Renew(Arg.Any<SingleWriterLeaseOwner>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()).Returns(false);
    }

    async Task Because()
    {
        await Observer.OnStart(CancellationToken.None);
        var stopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Lifetime.When(lifetime => lifetime.StopApplication()).Do(_ => stopped.TrySetResult());
        Time.Advance(TimeSpan.FromSeconds(10));
        await stopped.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        Time.Advance(TimeSpan.Zero);
    }

    [Fact] void should_stop_the_host() => Lifetime.Received(1).StopApplication();
    [Fact] void should_clear_readiness() => Keeper.IsHeld.ShouldBeFalse();
    [Fact] void should_fence_immediately() => FailFast.Received(1).FailFast(Arg.Any<string>());
    [Fact] void should_use_a_failure_exit_code() => Environment.ExitCode.ShouldEqual(1);
}
