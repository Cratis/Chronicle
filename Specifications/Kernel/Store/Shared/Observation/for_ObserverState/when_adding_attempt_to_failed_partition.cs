// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Store.Observation.for_ObserverState;

public class when_adding_attempt_to_failed_partition : given.a_failed_partition
{
    void Because() => state.AddAttemptToFailedPartition(partition);

    [Fact] void should_have_two_attempts() => state.FailedPartitions.First().Attempts.ShouldEqual(2);
}
