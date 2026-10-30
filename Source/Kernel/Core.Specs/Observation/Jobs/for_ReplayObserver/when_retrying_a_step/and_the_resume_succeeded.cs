// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver.when_retrying_a_step;

public class and_the_resume_succeeded : given.a_running_reactor_replay
{
    bool _resumed;

    async Task Establish()
    {
        await FailByTimeout(0);
        _resumed = await ResumeWithObserverReplaying();
    }

    async Task Because() => await _silo.TimerRegistry.FireAllAsync();

    [Fact] void should_have_resumed() => _resumed.ShouldBeTrue();
    [Fact] void should_retry_the_step() => RetriesOf(0).ShouldEqual(1);
}
