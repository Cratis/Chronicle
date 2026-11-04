// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.for_ContainerStartup.when_starting;

public class and_the_exit_state_cannot_be_read : given.a_container_startup
{
    void Establish()
    {
        Fail(_containers[0], Crash(_containers[0]));
        _inspect = (_, _) => throw new IOException("inspect unavailable");
    }

    Task Because() => Start();

    [Fact] void should_retry() => _result.ShouldEqual(_containers[1]);
    [Fact] void should_report_the_unavailable_state() => _reports.Single().ShouldContain("exit state unavailable");
}
