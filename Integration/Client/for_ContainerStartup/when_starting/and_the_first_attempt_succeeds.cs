// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.for_ContainerStartup.when_starting;

public class and_the_first_attempt_succeeds : given.a_container_startup
{
    Task Because() => Start();

    [Fact] void should_return_the_first_container() => _result.ShouldEqual(_containers[0]);
    [Fact] void should_call_built_once() => _built.ShouldContainOnly(_containers[0]);
    [Fact] void should_not_collect_evidence_or_dispose() => _calls.ShouldBeEmpty();
    [Fact] void should_not_report_a_retry() => _reports.ShouldBeEmpty();
}
