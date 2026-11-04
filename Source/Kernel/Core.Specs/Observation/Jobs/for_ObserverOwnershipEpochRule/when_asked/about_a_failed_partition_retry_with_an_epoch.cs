// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs.for_ObserverOwnershipEpochRule.when_asked;

public class about_a_failed_partition_retry_with_an_epoch : given.a_rule
{
    async Task Because() => _result = await Ask(new RetryFailedPartitionRequest(_observerKey, ObserverType.Reactor, _partition, EventSequenceNumber.First, []) { OwnershipEpoch = 7 });

    [Fact] void should_admit_it() => _result.ShouldBeTrue();
}
