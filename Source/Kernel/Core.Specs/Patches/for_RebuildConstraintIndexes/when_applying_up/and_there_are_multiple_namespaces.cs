// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.Jobs;
using Cratis.Monads;

namespace Cratis.Chronicle.Core.Specs.Patches.for_RebuildConstraintIndexes.when_applying_up;

public class and_there_are_multiple_namespaces : given.a_rebuild_constraint_indexes_patch
{
    EventStoreNamespaceName _otherNamespace;
    IJobsManager _otherJobsManager;

    void Establish()
    {
        _otherNamespace = "other-namespace";
        _otherJobsManager = Substitute.For<IJobsManager>();

        _namespaces.GetAll().Returns(Task.FromResult<IEnumerable<EventStoreNamespaceName>>([_namespace, _otherNamespace]));
        _grainFactory.GetGrain<IJobsManager>(0, new JobsManagerKey(_eventStore, _otherNamespace)).Returns(_otherJobsManager);

        _constraintsStorage.GetDefinitions().Returns(Task.FromResult<IEnumerable<IConstraintDefinition>>(
            [new UniqueConstraintDefinition("SomeUniqueConstraint", [new("SomeEvent", ["SomeProperty"])])]));

        _jobsManager
            .Start<IReindexConstraints, ReindexConstraintsRequest>(Arg.Any<ReindexConstraintsRequest>())
            .Returns(Task.FromResult(Result<JobId, StartJobError>.Success(JobId.New())));
        _otherJobsManager
            .Start<IReindexConstraints, ReindexConstraintsRequest>(Arg.Any<ReindexConstraintsRequest>())
            .Returns(Task.FromResult(Result<JobId, StartJobError>.Success(JobId.New())));
    }

    async Task Because() => await _patch.Up();

    [Fact] void should_start_reindex_job_for_the_first_namespace() =>
        _jobsManager.Received(1).Start<IReindexConstraints, ReindexConstraintsRequest>(Arg.Any<ReindexConstraintsRequest>());

    [Fact] void should_start_reindex_job_for_the_other_namespace() =>
        _otherJobsManager.Received(1).Start<IReindexConstraints, ReindexConstraintsRequest>(Arg.Any<ReindexConstraintsRequest>());
}
