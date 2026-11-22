// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_RetryFailedPartitions;

public class when_no_failed_partitions_exist : given.a_retry_command
{
    Exception _error;

    void Establish() => HaveFailedPartitions(0);

    async Task Because() => _error = await Catch.Exception(() =>
        new RetryFailedPartitions("some-event-store", "some-namespace", "some-observer", string.Empty).Handle(_grainFactory, _storage));

    [Fact] void should_fail_the_command() => _error.ShouldBeOfExactType<FailedPartitionsRecoveryCouldNotStart>();
    [Fact] void should_identify_the_observer() => _error.Message.ShouldContain("some-observer");
    [Fact] void should_not_accept_a_job() => _jobsManager.DidNotReceive().Start<IObserverOperatorCommandJob, ObserverOperatorCommandRequest>(
        Arg.Any<ObserverOperatorCommandRequest>());
}
