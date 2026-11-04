// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.for_ContainerStartup.when_starting;

public class and_the_log_tail_does_not_arrive_in_time : given.a_container_startup
{
    void Establish()
    {
        Fail(_containers[0], Crash(_containers[0]));
        _inspect = (_, _) => Task.FromResult<ContainerExit?>(new(1, false, ""));
        _readTail = (_, token) => new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously).Task.WaitAsync(token);
    }

    Task Because() => Start();

    [Fact] void should_retry_after_the_deadline() => _result.ShouldEqual(_containers[1]);
    [Fact] void should_report_the_unavailable_tail() => _reports.Single().ShouldContain("log tail unavailable");
}
