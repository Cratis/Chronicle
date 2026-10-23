// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition.when_completing;

public class should_release_the_subscriber : given.a_partition_step_with_a_projection_subscriber
{
    Catch<JobStepResult> _result;
    CancellationToken _cancellation = CancellationToken.None;

    async Task Because() => _result = await _jobStep.InvokePerformStep(_performState, _cancellation);

    [Fact] async Task should_request_release_once() => await _projectionSubscriber.Received(1).ReleaseAfterPartition();
    [Fact] void should_still_complete() => _result.Match(result => !result.TryGetError(out _), _ => false).ShouldBeTrue();
}
