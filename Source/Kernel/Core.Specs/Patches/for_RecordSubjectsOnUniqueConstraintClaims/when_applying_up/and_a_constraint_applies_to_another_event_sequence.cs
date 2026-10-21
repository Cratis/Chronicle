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

/// <summary>
/// Each event sequence keeps its own constraint index, so a constraint covering personal data is rebuilt in every
/// sequence it applies to - not only the event log.
/// </summary>
public class and_a_constraint_applies_to_another_event_sequence : given.a_record_subjects_on_unique_constraint_claims_patch
{
    static readonly EventSequenceId _otherSequence = "some-other-sequence";
    UniqueConstraintDefinition _otherSequenceOnly;

    void Establish()
    {
        _otherSequenceOnly = new UniqueConstraintDefinition("UniqueEmail", [new("PersonRegistered", ["Email"])])
        {
            EventSequences = [_otherSequence]
        };

        _constraintsStorage.GetDefinitions().Returns(Task.FromResult<IEnumerable<IConstraintDefinition>>([_otherSequenceOnly]));
        _eventSequences.GetEventSequences().Returns(Task.FromResult<IEnumerable<EventSequenceId>>([EventSequenceId.Log, _otherSequence]));

        var personalEmail = JsonSchema.FromJson("{\"type\":\"object\",\"properties\":{\"email\":{\"type\":\"string\"}}}");
        personalEmail.Properties["email"].ExtensionData = new Dictionary<string, object?>
        {
            [ComplianceJsonSchemaExtensions.ComplianceKey] = new[] { new ComplianceSchemaMetadata("PII", string.Empty) }
        };

        _eventTypesStorage.GetAllGenerationsForEventType(Arg.Any<EventType>()).Returns(Task.FromResult<IEnumerable<EventTypeSchema>>(
        [
            new(new("PersonRegistered", 1), EventTypeOwner.Client, EventTypeSource.Code, personalEmail)
        ]));

        _jobsManager
            .Start<IReindexConstraints, ReindexConstraintsRequest>(Arg.Any<ReindexConstraintsRequest>())
            .Returns(Task.FromResult(Result<JobId, StartJobError>.Success(JobId.New())));
    }

    async Task Because() => await _patch.Up();

    [Fact] void should_reindex_the_sequence_the_constraint_applies_to() =>
        _jobsManager.Received(1).Start<IReindexConstraints, ReindexConstraintsRequest>(
            Arg.Is<ReindexConstraintsRequest>(_ => _.EventSequenceId == _otherSequence && _.Changes.Any(change => change.Name == _otherSequenceOnly.Name)));

    [Fact] void should_not_reindex_the_event_log() =>
        _jobsManager.DidNotReceive().Start<IReindexConstraints, ReindexConstraintsRequest>(
            Arg.Is<ReindexConstraintsRequest>(_ => _.EventSequenceId == EventSequenceId.Log));
}
