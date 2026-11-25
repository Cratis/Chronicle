// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition.when_performing;

/// <summary>
/// The failure is reported from the state the step was performed with, not from the grain's persisted state,
/// which the step must not read off the activation's scheduler (Cratis/Chronicle#5804).
/// </summary>
public class and_an_unexpected_exception_is_thrown_for_a_retry_episode : given.a_performing_job_step
{
    FailedPartitionId _episodeId;
    Guid _deferralId;

    void Establish()
    {
        _episodeId = new(Guid.NewGuid());
        _deferralId = Guid.NewGuid();
        _performState.EpisodeId = _episodeId;
        _performState.DeferralId = _deferralId;
        _eventTypesStorage
            .GetFor(Arg.Any<IEnumerable<EventType>>())
            .Returns(Task.FromException<IEnumerable<EventTypeSchema>>(new InvalidOperationException("storage is down")));
    }

    async Task Because() => await _jobStep.InvokePerformStep(_performState);

    [Fact]
    void should_report_the_failure_for_the_episode_the_step_was_performed_for() =>
        _observer.Received(1).PartitionFailed(
            Arg.Any<Key>(),
            _performState.StartEventSequenceNumber,
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<string>(),
            Arg.Any<FailureKind>(),
            _episodeId,
            _deferralId,
            Arg.Any<WebhookDeliveryFailurePolicy>());
}
