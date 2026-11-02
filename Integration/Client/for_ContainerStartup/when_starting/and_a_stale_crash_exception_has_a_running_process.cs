// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.for_ContainerStartup.when_starting;

public class and_a_stale_crash_exception_has_a_running_process : given.a_container_startup
{
    Exception _original;

    void Establish()
    {
        _original = Crash(_containers[0]);
        Fail(_containers[0], _original);
    }

    async Task Because() => _failure = await Catch.Exception(Start);

    [Fact] void should_trust_the_fresh_inspect() => _failure.ShouldEqual(_original);
    [Fact] void should_build_once() => _built.ShouldContainOnly(_containers[0]);
    [Fact] void should_not_dispose_the_running_container() => _calls.ShouldNotContain("dispose:container-1");
}
