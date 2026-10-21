// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.States;
using Cratis.Chronicle.Observation.Webhooks;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_a_gone_webhook_registration_resumes_retained_replay : given.a_gone_retired_webhook
{
    JobState _replay;
    JobState _partitionReplay;

    async Task Establish()
    {
        _replay = new JobState { Id = JobId.New(), Status = JobStatus.Stopped, Request = new ReplayObserverRequest(_observerKey, ObserverType.External, ["Event1", "Event2"]) };
        _partitionReplay = new JobState { Id = JobId.New(), Status = JobStatus.Stopped, Request = new ReplayObserverPartitionRequest(_observerKey, ObserverType.External, "retained-partition", 42UL, 100UL, ["Event1", "Event2"]) };
        _jobsManager.GetJobs(Arg.Any<JobQuery>()).Returns(_ => Task.FromResult<IImmutableList<JobState>>(ImmutableList.Create(_replay, _partitionReplay)));
        _jobsManager.Resume(Arg.Any<JobId>()).Returns(call =>
        {
            var job = call.Arg<JobId>() == _replay.Id ? _replay : _partitionReplay;
            if (job.Id == _replay.Id) _replay = new JobState { Id = job.Id, Request = job.Request, Status = JobStatus.Running };
            else _partitionReplay = new JobState { Id = job.Id, Request = job.Request, Status = JobStatus.Running };
            return Task.FromResult(true);
        });
        _stateStorage.State = _stateStorage.State with
        {
            IsReplaying = true,
            NextEventSequenceNumber = 84UL,
            HandledEventCount = 45UL,
            HandledEventCountPerEventType = new Dictionary<EventTypeId, EventCount> { ["Event1"] = 12UL },
            ReplayingPartitions = new HashSet<Key> { "retained-partition" }
        };
        _failedPartitionsState.Partitions.Single().RecoveryCursor = new(42UL, FailedPartitionRecoveryMode.Replay, 100UL);
        await Crash();
        _silo.AddProbe<IWebhookObserverSubscriber>(_ => _webhook);
    }

    async Task Because()
    {
        await RegisterAgain();
        await _observer.Subscribe<IWebhookObserverSubscriber>(ObserverType.External, ["Event1", "Event2"], SiloAddress.Zero, reactivateRetired: false, automatic: true);
        await Crash();
    }

    [Fact] void should_resume_the_same_full_replay() => _jobsManager.Received(1).Resume(_replay.Id);
    [Fact] void should_resume_the_same_partition_replay() => _jobsManager.Received(1).Resume(_partitionReplay.Id);
    [Fact] void should_not_start_a_new_replay() => _jobsManager.DidNotReceive().Start<IReplayObserver, ReplayObserverRequest>(Arg.Any<ReplayObserverRequest>());
    [Fact] async Task should_return_to_replay() => (await _observer.GetCurrentState()).ShouldBeOfExactType<Replay>();
    [Fact] void should_remember_retained_replay_across_restart() => _stateStorage.State.WebhookReplayRetained.ShouldBeTrue();
    [Fact] void should_preserve_the_global_cursor() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)84UL);
    [Fact] void should_preserve_the_handled_count() => _stateStorage.State.HandledEventCount.ShouldEqual((EventCount)45UL);
    [Fact] void should_preserve_per_type_counts() => _stateStorage.State.HandledEventCountPerEventType[(EventTypeId)"Event1"].ShouldEqual((EventCount)12UL);
    [Fact] void should_preserve_replaying_partitions() => _stateStorage.State.ReplayingPartitions.Single().ShouldEqual((Key)"retained-partition");
    [Fact] void should_preserve_the_failed_replay_boundary() => _failedPartitionsState.Partitions.Single().EffectiveRecoveryCursor.ReplayBoundary.ShouldEqual((EventSequenceNumber)100UL);
}
