// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DotNet.Testcontainers.Containers;
using NSubstitute;

namespace Cratis.Chronicle.Integration.for_ContainerStartup.when_starting;

public class and_the_first_attempt_exits : given.a_container_startup
{
    const string AssertLine = "ASSERT: Expression=(!currentWorker->IsPreemptiveOnEntry())";

    void Establish()
    {
        // The readiness exec can fail after Testcontainers cached a running state.
        _containers[0].State.Returns(TestcontainersStates.Running);
        Fail(_containers[0], Crash(_containers[0]));
        _inspect = (_, _) => Task.FromResult<ContainerExit?>(new(1, false, ""));
        _readTail = (_, _) => Task.FromResult(AssertLine);
    }

    Task Because() => Start();

    [Fact] void should_return_the_second_container() => _result.ShouldEqual(_containers[1]);
    [Fact] void should_call_built_for_both_containers() => _built.ShouldEqual(new[] { _containers[0], _containers[1] });
    [Fact] void should_collect_evidence_before_disposing_once() => _calls.ShouldEqual(new[] { "inspect:container-1", "tail:container-1", "dispose:container-1" });
    [Fact] void should_report_one_retry() => _reports.Count.ShouldEqual(1);
    [Fact] void should_name_the_attempt() => _reports.Single().ShouldContain("attempt 1/3");
    [Fact] void should_report_the_exit_code() => _reports.Single().ShouldContain("exit code 1");
    [Fact] void should_report_the_assert() => _reports.Single().ShouldContain(AssertLine);
    [Fact] void should_notify_the_retry_hook() => _retries.Count.ShouldEqual(1);
}
