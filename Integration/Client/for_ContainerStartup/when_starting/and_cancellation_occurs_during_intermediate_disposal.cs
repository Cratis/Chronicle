// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NSubstitute;
using NSubstitute.Core;
using NSubstitute.Extensions;

namespace Cratis.Chronicle.Integration.for_ContainerStartup.when_starting;

public class and_cancellation_occurs_during_intermediate_disposal : given.a_container_startup
{
    async Task Because()
    {
        using var cancellation = new CancellationTokenSource();
        Fail(_containers[0], Crash(_containers[0]));
        _inspect = (_, _) => Task.FromResult<ContainerExit?>(new(1, false, ""));
#pragma warning disable CA2012 // Configures a substitute; no real ValueTask is being consumed.
        _containers[0].Configure().DisposeAsync().Returns(new Func<CallInfo, ValueTask>(async _ => await cancellation.CancelAsync()));
#pragma warning restore CA2012
        _failure = await Catch.Exception(() => Start(cancellation.Token));
    }

    [Fact] void should_preserve_the_cancellation() => _failure.ShouldBeOfExactType<OperationCanceledException>();
    [Fact] void should_not_build_another_container() => _built.ShouldContainOnly(_containers[0]);
    [Fact] void should_not_retry() => _retries.ShouldBeEmpty();
}
