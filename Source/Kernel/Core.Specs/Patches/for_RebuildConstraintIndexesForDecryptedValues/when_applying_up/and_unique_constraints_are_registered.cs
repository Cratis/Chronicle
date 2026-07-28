// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.Jobs;
using Cratis.Monads;

namespace Cratis.Chronicle.Core.Specs.Patches.for_RebuildConstraintIndexesForDecryptedValues.when_applying_up;

public class and_unique_constraints_are_registered : given.a_rebuild_constraint_indexes_for_decrypted_values_patch
{
    UniqueConstraintDefinition _uniqueConstraint;

    void Establish()
    {
        _uniqueConstraint = new UniqueConstraintDefinition("SomeUniqueConstraint", [new("SomeEvent", ["SomeProperty"])]);

        _constraintsStorage.GetDefinitions().Returns(Task.FromResult<IEnumerable<IConstraintDefinition>>([_uniqueConstraint]));

        _jobsManager
            .Start<IReindexConstraints, ReindexConstraintsRequest>(Arg.Any<ReindexConstraintsRequest>())
            .Returns(Task.FromResult(Result<JobId, StartJobError>.Success(JobId.New())));
    }

    async Task Because() => await _patch.Up();

    [Fact] void should_start_reindex_job_for_the_namespace() =>
        _jobsManager.Received(1).Start<IReindexConstraints, ReindexConstraintsRequest>(Arg.Any<ReindexConstraintsRequest>());

    [Fact] void should_reindex_the_log_event_sequence() =>
        _jobsManager.Received(1).Start<IReindexConstraints, ReindexConstraintsRequest>(
            Arg.Is<ReindexConstraintsRequest>(_ => _.EventSequenceId == EventSequenceId.Log));

    [Fact] void should_mark_the_unique_constraint_as_requiring_reindex() =>
        _jobsManager.Received(1).Start<IReindexConstraints, ReindexConstraintsRequest>(
            Arg.Is<ReindexConstraintsRequest>(_ =>
                _.Changes.Count == 1 &&
                _.Changes.Single().Name == _uniqueConstraint.Name &&
                _.Changes.Single().RequiresReindex));
}
