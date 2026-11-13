// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_KernelRun.when_running;

public class and_the_host_stops_cleanly : given.a_host
{
    KernelRunResult _result;

    void Establish() => BuildHost(_ => { });

    async Task Because() => _result = await Run();

    [Fact] void should_be_completed() => _result.Outcome.ShouldEqual(KernelRunOutcome.Completed);
    [Fact] void should_not_have_a_failure() => _result.Failure.ShouldBeNull();
}
