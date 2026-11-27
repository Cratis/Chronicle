// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.for_Observer.when_catch_up_round_completes;

public class and_a_failed_partition_retry_precedes_successor_preparation : given.an_observer_with_a_handover_log
{
    RetryFailedPartitionRequest? _retry;
    EventSequenceNumber _retryFrom = EventSequenceNumber.Unavailable;
    bool _startedBeforeTheOtherRepairFinished;
    bool _failedDuringSuccessorRead;
    bool _healthyProgress;
    bool _recovered;

    protected virtual bool DeliversPartitionPages => false;

    void Establish()
    {
        _definitionStorage.State = _definitionStorage.State with { Type = ObserverType.Reactor, EventTypes = [event_type] };
        _stateStorage.State = _stateStorage.State with
        {
            LastHandledEventSequenceNumber = 13UL,
            NextEventSequenceNumber = 14UL,
            PendingCatchUpRepairs = [new("P", 11UL, 14UL), new("N", 12UL, 14UL)]
        };
        Append("P", 11UL);
        Append("N", 12UL);
        Append("P", 13UL);
        Append("Q", 16UL);
        Append("Q", 17UL);
        Append("P", 18UL);
        _jobsManager.Start<IRetryFailedPartition, RetryFailedPartitionRequest>(Arg.Any<RetryFailedPartitionRequest>())
            .Returns(call =>
            {
                _retry = call.Arg<RetryFailedPartitionRequest>();
                _retryFrom = _retry.FromSequenceNumber;
                return Result<JobId, StartJobError>.Success(JobId.New());
            });
    }

    async Task Because()
    {
        await _observer.CatchUp();
        await _silo.RunCatchUpContinuation();
        var p = _repairs.Single(_ => _.Key == (Key)"P");
        await _observer.HandoverRepairCaughtUp(p.Key, p.FromSequenceNumber, p.ToSequenceNumber.Next(), 11UL, succeeded: false, p.OwnershipEpoch);
        await _observer.TryStartRecoverJobForFailedPartition("P");
        _startedBeforeTheOtherRepairFinished = _retry is not null;
        await FinishRepair(_repairs.Single(_ => _.Key == (Key)"N"));
        await _silo.RunCatchUpContinuation();
        if (_wide is { } wide) await RunSuccessor(wide);
        await _silo.RunCatchUpContinuation();
        if (!_recovered && _retry is null) await _observer.TryStartRecoverJobForFailedPartition("P");
        await FinishAdmittedRetry();
    }

    [Fact] void should_not_start_retry_while_another_range_is_outstanding() => _startedBeforeTheOtherRepairFinished.ShouldBeFalse();
    [Fact] void should_keep_the_failed_partition_owned_by_its_episode_while_reading() => _failedDuringSuccessorRead.ShouldBeTrue();
    [Fact] void should_allow_healthy_work_to_progress() => _healthyProgress.ShouldBeTrue();
    [Fact] void should_eventually_start_recovery_at_the_first_unhandled_position() => _retryFrom.ShouldEqual((EventSequenceNumber)12UL);
    [Fact] void should_deliver_p_eighteen_only_once() => _delivered.Count(_ => _ == (EventSequenceNumber)18UL).ShouldEqual(1);
    [Fact] void should_clear_the_failed_episode_after_recovery() => _failedPartitionsStorage.State.Partitions.ShouldBeEmpty();

    async Task RunSuccessor(CatchUpObserverRequest request)
    {
        var page = DeliversPartitionPages ? SetUpPageSubscriber() : null;
        var steps = await given.handover_successor_reader.Prepare(request, _observer, ["P", "Q"]);
        await FinishAdmittedRetry();
        _failedDuringSuccessorRead = (await _observer.GetFailedPartitionKeys()).Contains((Key)"P");
        if (page is not null)
        {
            await given.handover_successor_reader.WalkPages((HandleEventsForObserverArguments)steps.Single().Request, _observer, _eventSequenceStorage, page);
        }
        else
        {
            foreach (var step in steps.Select(_ => (HandleEventsForPartitionArguments)_.Request))
            {
                var last = await DeliverRange(step.StartEventSequenceNumber, step.EndEventSequenceNumber, step.EventTypes, step.Partition);
                await _observer.PartitionCaughtUp(step.Partition, last, last);
            }
        }
        _healthyProgress = _delivered.Contains(17UL);
        await _observer.CaughtUp(JobId.NotSet, 17UL, 18UL);
    }

    IObserverPartitionPageSubscriber SetUpPageSubscriber()
    {
        var page = Substitute.For<IObserverPartitionPageSubscriber>();
        page.OnNextPartitions(Arg.Any<IReadOnlyList<ObserverPartitionBatch>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(call => new ObserverPageResult(call.Arg<IReadOnlyList<ObserverPartitionBatch>>().ToDictionary(_ => _.Partition, _ => Delivered(_.Events))));
        page.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(call => Delivered(call.Arg<IEnumerable<AppendedEvent>>()));
        _silo.AddProbe(_ => page);
        _observer.SetSubscription(subscription with { SubscriberType = typeof(IObserverPartitionPageSubscriber) });
        return page;
    }

    async Task FinishAdmittedRetry()
    {
        if (_retry is not { } retry) return;
        _retry = null;
        var last = await DeliverRange(retry.FromSequenceNumber, EventSequenceNumber.Max, retry.EventTypes, retry.Key);
        await _observer.FailedPartitionRecovered(retry.Key, last, last, retry.EpisodeId, retry.DeferralId);
        _recovered = true;
    }
}
