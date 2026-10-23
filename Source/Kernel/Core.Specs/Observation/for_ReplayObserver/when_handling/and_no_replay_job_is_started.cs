// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_ReplayObserver.when_handling;

public class and_no_replay_job_is_started : given.a_replay_command
{
    void Establish() => _observer.Replay().Returns(JobId.NotSet);

    async Task Because() => _result = await _scenario.Execute(new ReplayObserver(
        "some-event-store", "some-namespace", "some-observer", "event-log"));

    [Fact] void should_fail() => _result.IsSuccess.ShouldBeFalse();

    [Fact] void should_explain_why_replay_was_not_started() =>
        _result.ValidationResults.Any(_ => _.Message == ReplayObserver.ReplayNotStartedMessage).ShouldBeTrue();
}
