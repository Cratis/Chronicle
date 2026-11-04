// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.for_ContainerStartup.when_starting;

public class and_the_failure_leaves_the_container_running : given.a_container_startup
{
    readonly TimeoutException _original = new("readiness timed out");

    void Establish() => Fail(_containers[0], _original);

    async Task Because() => _failure = await Catch.Exception(Start);

    [Fact] void should_rethrow_the_original_failure() => _failure.ShouldEqual(_original);
    [Fact] void should_build_once() => _built.ShouldContainOnly(_containers[0]);
    [Fact] void should_leave_disposal_to_the_caller() => _calls.ShouldNotContain("dispose:container-1");
    [Fact] void should_not_report_a_retry() => _reports.ShouldBeEmpty();
}
