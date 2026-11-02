// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.for_ContainerStartup.when_starting;

public class and_inspection_times_out_for_a_non_crash_failure : given.a_container_startup
{
    readonly TimeoutException _original = new("readiness timed out");

    void Establish()
    {
        Fail(_containers[0], _original);
        _inspect = (_, _) => new TaskCompletionSource<ContainerExit?>(TaskCreationOptions.RunContinuationsAsynchronously).Task;
    }

    async Task Because() => _failure = await Catch.Exception(Start);

    [Fact] void should_preserve_the_startup_failure() => _failure.ShouldEqual(_original);
    [Fact] void should_build_once() => _built.ShouldContainOnly(_containers[0]);
    [Fact] void should_not_dispose_without_proof_of_exit() => _calls.ShouldNotContain("dispose:container-1");
}
