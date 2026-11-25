// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_ReplayObserver.when_handling;

public class and_no_replay_job_is_started : given.a_replay_command
{
    void Establish() => _definitions.Get(_key.ObserverId).Returns(new ObserverDefinition(
        _key.ObserverId, [], _key.EventSequenceId, ObserverType.Reactor, ObserverOwner.Client, false));

    async Task Because() => _result = await _scenario.Execute(new ReplayObserver(
        "some-event-store", "some-namespace", "some-observer", "event-log"));

    [Fact] void should_fail() => _result.IsSuccess.ShouldBeFalse();

    [Fact] void should_explain_why_replay_was_not_started() =>
        _result.ValidationResults.Any(_ => _.Message == ReplayObserver.ReplayNotStartedMessage).ShouldBeTrue();

    [Fact] void should_not_accept_a_job() => _jobsManager.DidNotReceive().Start<IObserverOperatorCommandJob, ObserverOperatorCommandRequest>(
        Arg.Any<ObserverOperatorCommandRequest>());
}
