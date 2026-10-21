// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver.when_checking_if_it_can_resume;

public class and_it_has_no_request : for_ReplayObserver.given.a_replay_observer_job
{
    bool _canResume;

    async Task Because() => _canResume = await _job.CanResumeForTesting();

    [Fact] void should_not_be_able_to_resume() => _canResume.ShouldBeFalse();
    [Fact] async Task should_not_ask_whether_the_observer_is_subscribed() => await _observer.DidNotReceive().IsSubscribed();
}
