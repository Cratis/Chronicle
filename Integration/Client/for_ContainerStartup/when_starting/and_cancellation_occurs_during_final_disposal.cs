// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NSubstitute;

namespace Cratis.Chronicle.Integration.for_ContainerStartup.when_starting;

public class and_cancellation_occurs_during_final_disposal : given.a_container_startup
{
    async Task Because()
    {
        using var cancellation = new CancellationTokenSource();
        foreach (var container in _containers)
        {
            Fail(container, Crash(container));
        }
        _inspect = (_, _) => Task.FromResult<ContainerExit?>(new(1, false, ""));
#pragma warning disable CA2012 // Configures a substitute; no real ValueTask is being consumed.
        _containers[2].DisposeAsync().Returns(_ =>
        {
            cancellation.Cancel();
            return ValueTask.FromException(new IOException("disposal failed"));
        });
#pragma warning restore CA2012
        _failure = await Catch.Exception(() => Start(cancellation.Token));
    }

    [Fact] void should_preserve_the_cancellation() => _failure.ShouldBeOfExactType<OperationCanceledException>();
    [Fact] void should_not_build_beyond_the_final_attempt() => _built.ShouldEqual(_containers);
    [Fact] void should_only_retry_the_intermediate_attempts() => _retries.Count.ShouldEqual(2);
}
