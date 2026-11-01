// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.for_ContainerStartup.when_starting;

public class and_startup_is_cancelled : given.a_container_startup
{
    async Task Because()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        Fail(_containers[0], new OperationCanceledException(cancellation.Token));
        _failure = await Catch.Exception(() => Start(cancellation.Token));
    }

    [Fact] void should_preserve_the_cancellation() => _failure.ShouldBeOfExactType<OperationCanceledException>();
    [Fact] void should_not_collect_evidence_or_retry() => _calls.ShouldBeEmpty();
    [Fact] void should_not_build() => _built.ShouldBeEmpty();
}
