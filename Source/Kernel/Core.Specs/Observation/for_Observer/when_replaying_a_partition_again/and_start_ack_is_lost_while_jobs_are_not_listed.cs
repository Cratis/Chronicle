// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;
using NSubstitute.Extensions;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.for_Observer.when_replaying_a_partition_again;

public class and_start_ack_is_lost_while_jobs_are_not_listed : given.an_observer_with_subscription_for_specific_event_type
{
    static readonly Key _partition = "mutated-source";
    readonly List<(JobId Id, ulong? Epoch)> _starts = [];
    readonly TaskCompletionSource<JobDrainStatus> _drain = new(TaskCreationOptions.RunContinuationsAsynchronously);
    string? _addressedJob;
    ulong? _persistedFence;
    int _startsBeforeDrain;
    bool _intentSurvived;

    protected override void ConfigureProbes(TestKitSilo silo)
    {
        var job = Substitute.For<IReplayObserverPartition, IJobDrain>();
        ((IJobDrain)job).StopAndDrain(Arg.Any<TimeSpan>()).Returns(_ =>
        {
            _persistedFence = _stateStorage.State.PartitionClaims!.Single(claim => claim.Partition == _partition).Epoch.Value;
            return _drain.Task;
        });
        silo.AddProbe<IReplayObserverPartition>(key =>
        {
            _addressedJob = key.ToString();
            return job;
        });
    }

    void Establish()
    {
        _jobsManager.Start<IReplayObserverPartition, ReplayObserverPartitionRequest>(Arg.Any<ReplayObserverPartitionRequest>())
            .Returns(Task.FromException<Result<JobId, StartJobError>>(new TimeoutException()));
        _jobsManager.Configure().Start<IReplayObserverPartition, ReplayObserverPartitionRequest>(Arg.Any<JobId>(), Arg.Any<ReplayObserverPartitionRequest>())
            .Returns(call =>
            {
                var id = call.Arg<JobId>();
                _starts.Add((id, call.Arg<ReplayObserverPartitionRequest>().OwnershipEpoch));
                return _starts.Count == 1
                    ? Task.FromException<Result<JobId, StartJobError>>(new TimeoutException())
                    : Task.FromResult(Result<JobId, StartJobError>.Success(id));
            });
    }

    async Task Because()
    {
        _ = await Cratis.Specifications.Catch.Exception(() => _observer.ReplayPartition(_partition));
        _intentSurvived = _stateStorage.State.ReplayingPartitions.Contains(_partition);
        await _observer.CheckHealth();
        _startsBeforeDrain = _starts.Count;
        _drain.SetResult(JobDrainStatus.Drained);
        await _observer.CheckHealth();
    }

    [Fact] void should_keep_the_hold_after_the_missing_ack() => _intentSurvived.ShouldBeTrue();
    [Fact] void should_address_the_unlisted_job() => _addressedJob!.ShouldContain(_starts[0].Id.Value.ToString());
    [Fact] void should_have_persisted_a_new_epoch_before_the_exact_job_check() => (_persistedFence > _starts[0].Epoch).ShouldBeTrue();
    [Fact] void should_wait_for_the_addressed_job_turn() => _startsBeforeDrain.ShouldEqual(1);
    [Fact] void should_submit_a_single_fresh_id_after_drain() => _starts.Select(start => start.Id).Distinct().Count().ShouldEqual(2);
}
