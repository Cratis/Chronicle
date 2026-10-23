// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.Schemas;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Core.Specs.Patches.for_RecordSubjectsOnUniqueConstraintClaims.when_applying_up;

public class and_unique_constraints_are_registered : given.a_record_subjects_on_unique_constraint_claims_patch
{
    UniqueConstraintDefinition _personalInLaterGeneration;
    UniqueConstraintDefinition _notPersonal;

    void Establish()
    {
        _personalInLaterGeneration = new UniqueConstraintDefinition("UniqueEmail", [new("PersonRegistered", ["Email"])]);
        _notPersonal = new UniqueConstraintDefinition("UniqueCode", [new("ProductAdded", ["Code"])]);

        _constraintsStorage.GetDefinitions().Returns(Task.FromResult<IEnumerable<IConstraintDefinition>>([_personalInLaterGeneration, _notPersonal]));

        var personalEmail = JsonSchema.FromJson("{\"type\":\"object\",\"properties\":{\"email\":{\"type\":\"string\"}}}");
        personalEmail.Properties["email"].ExtensionData = new Dictionary<string, object?>
        {
            [ComplianceJsonSchemaExtensions.ComplianceKey] = new[] { new ComplianceSchemaMetadata("PII", string.Empty) }
        };
        var plainEmail = JsonSchema.FromJson("{\"type\":\"object\",\"properties\":{\"email\":{\"type\":\"string\"}}}");
        var plainCode = JsonSchema.FromJson("{\"type\":\"object\",\"properties\":{\"code\":{\"type\":\"string\"}}}");

        _eventTypesStorage.GetAllGenerationsForEventType(Arg.Is<EventType>(_ => _.Id == "PersonRegistered")).Returns(Task.FromResult<IEnumerable<EventTypeSchema>>(
        [
            new(new("PersonRegistered", 1), EventTypeOwner.Client, EventTypeSource.Code, plainEmail),
            new(new("PersonRegistered", 2), EventTypeOwner.Client, EventTypeSource.Code, personalEmail)
        ]));
        _eventTypesStorage.GetAllGenerationsForEventType(Arg.Is<EventType>(_ => _.Id == "ProductAdded")).Returns(Task.FromResult<IEnumerable<EventTypeSchema>>(
        [
            new(new("ProductAdded", 1), EventTypeOwner.Client, EventTypeSource.Code, plainCode)
        ]));

        _jobsManager
            .Start<IReindexConstraints, ReindexConstraintsRequest>(Arg.Any<ReindexConstraintsRequest>())
            .Returns(Task.FromResult(Result<JobId, StartJobError>.Success(JobId.New())));
    }

    async Task Because() => await _patch.Up();

    [Fact] void should_reindex_the_log_event_sequence_once() =>
        _jobsManager.Received(1).Start<IReindexConstraints, ReindexConstraintsRequest>(
            Arg.Is<ReindexConstraintsRequest>(_ => _.EventSequenceId == EventSequenceId.Log));

    [Fact] void should_reindex_only_the_constraint_covering_personal_data_in_any_generation() =>
        _jobsManager.Received(1).Start<IReindexConstraints, ReindexConstraintsRequest>(
            Arg.Is<ReindexConstraintsRequest>(_ =>
                _.Changes.Count == 1 &&
                _.Changes.All(change => change.RequiresReindex) &&
                _.Changes.Any(change => change.Name == _personalInLaterGeneration.Name)));
}
