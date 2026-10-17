// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_RetryFailedPartition.when_preparing_recovery_steps;

public class and_the_request_carries_a_scope : given.a_retry_failed_partition_job
{
    ObserverReaderScope _scope;
    IImmutableList<JobStepDetails> _steps;

    void Establish()
    {
        _scope = ObserverReaderScope.Wildcard;
        _request = _request with { ReaderScope = _scope };
    }

    async Task Because() => _steps = await _job.PrepareRecoverySteps(_request);

    [Fact] void should_hand_the_scope_to_the_step() => ((HandleEventsForPartitionArguments)_steps[0].Request).ReaderScope.ShouldBeSame(_scope);
}
