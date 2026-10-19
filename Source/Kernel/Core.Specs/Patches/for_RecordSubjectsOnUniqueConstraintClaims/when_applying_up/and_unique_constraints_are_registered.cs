// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Core.Specs.Patches.for_RecordSubjectsOnUniqueConstraintClaims.when_applying_up;

public class and_unique_constraints_are_registered : given.a_record_subjects_on_unique_constraint_claims_patch
{
    UniqueConstraintDefinition _personal;
    UniqueConstraintDefinition _other;

    void Establish()
    {
        _personal = new UniqueConstraintDefinition("UniqueEmail", [new("PersonRegistered", ["Email"])]);
        _other = new UniqueConstraintDefinition("UniqueCode", [new("ProductAdded", ["Code"])]);

        _constraintsStorage.GetDefinitions().Returns(Task.FromResult<IEnumerable<IConstraintDefinition>>([_personal, _other]));

        _jobsManager
            .Start<IReindexConstraints, ReindexConstraintsRequest>(Arg.Any<ReindexConstraintsRequest>())
            .Returns(Task.FromResult(Result<JobId, StartJobError>.Success(JobId.New())));
    }

    async Task Because() => await _patch.Up();

    [Fact] void should_reindex_the_log_event_sequence_once() =>
        _jobsManager.Received(1).Start<IReindexConstraints, ReindexConstraintsRequest>(
            Arg.Is<ReindexConstraintsRequest>(_ => _.EventSequenceId == EventSequenceId.Log));

    [Fact] void should_reindex_every_unique_constraint() =>
        _jobsManager.Received(1).Start<IReindexConstraints, ReindexConstraintsRequest>(
            Arg.Is<ReindexConstraintsRequest>(_ =>
                _.Changes.Count == 2 &&
                _.Changes.All(change => change.RequiresReindex) &&
                _.Changes.Any(change => change.Name == _personal.Name) &&
                _.Changes.Any(change => change.Name == _other.Name)));
}
