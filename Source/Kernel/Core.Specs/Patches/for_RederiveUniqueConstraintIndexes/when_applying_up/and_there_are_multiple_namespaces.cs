// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Events.Constraints;
using Cratis.Monads;

namespace Cratis.Chronicle.Core.Specs.Patches.for_RederiveUniqueConstraintIndexes.when_applying_up;

public class and_there_are_multiple_namespaces : given.a_rederive_patch
{
    IJobsManager _otherJobsManager;

    void Establish()
    {
        EventStoreNamespaceName other = "other-namespace";
        _otherJobsManager = Substitute.For<IJobsManager>();
        _otherJobsManager
            .Start<IReindexConstraints, ReindexConstraintsRequest>(Arg.Any<ReindexConstraintsRequest>())
            .Returns(Task.FromResult(Result<JobId, StartJobError>.Success(JobId.New())));
        _namespaces.GetAll().Returns(Task.FromResult<IEnumerable<EventStoreNamespaceName>>([_namespace, other]));
        _grainFactory.GetGrain<IJobsManager>(0, new JobsManagerKey(_eventStore, other)).Returns(_otherJobsManager);
        _grainFactory.GetGrain<IEventSequences>(0, new EventSequencesKey(_eventStore, other)).Returns(_sequences);
        var namespaceStorage = Substitute.For<IEventStoreNamespaceStorage>();
        var pending = Substitute.For<IUniqueConstraintsStorage>();
        pending.CountPendingSpellings().Returns(Task.FromResult(1));
        namespaceStorage.GetUniqueConstraintsStorage(Arg.Any<EventSequenceId>()).Returns(pending);
        _eventStoreStorage.GetNamespace(other).Returns(namespaceStorage);
    }

    async Task Because() => await _patch.Up();

    [Fact] void should_not_start_a_job_in_the_namespace_without_pending_rows() => _jobsManager.DidNotReceive().Start<IReindexConstraints, ReindexConstraintsRequest>(Arg.Any<ReindexConstraintsRequest>());
    [Fact] void should_start_a_job_for_each_sequence_in_the_namespace_with_pending_rows() => _otherJobsManager.Received(2).Start<IReindexConstraints, ReindexConstraintsRequest>(Arg.Any<ReindexConstraintsRequest>());
}
