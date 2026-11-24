// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_RetryFailedPartitions;

public class when_handling : given.a_retry_command
{
    RetryFailedPartitionsResult _result;

    void Establish() => HaveFailedPartitions(2);

    async Task Because() => _result = await new RetryFailedPartitions("some-event-store", "some-namespace", "some-observer", string.Empty).Handle(_grainFactory, _storage);

    [Fact] void should_tell_how_many_partitions_were_failed() => _result.FailedPartitions.ShouldEqual(2);

    [Fact] void should_accept_the_retry_as_a_job_for_the_observers_inbox() =>
        _jobsManager.Received(1).Start<IObserverOperatorCommandJob, ObserverOperatorCommandRequest>(
            Arg.Is<ObserverOperatorCommandRequest>(_ => _.ObserverKey == _key && _.Command == ObserverOperatorCommand.RetryFailedPartitions));

    [Fact] void should_not_ask_the_observer() => _observer.ReceivedCalls().ShouldBeEmpty();
}
