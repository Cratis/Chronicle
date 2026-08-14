// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402

using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;
using MongoDB.Bson;
using context = Cratis.Chronicle.Integration.for_ReadModels.when_a_projection_joins_pii_from_another_subject.and_the_joined_subject_is_erased.context;

namespace Cratis.Chronicle.Integration.for_ReadModels.when_a_projection_joins_pii_from_another_subject;

/// <summary>
/// Verifies that a stored projection keeps each PII property inside the erasure reach of the event subject it came from.
/// </summary>
/// <param name="context">The test context.</param>
[Collection(ChronicleCollection.Name)]
public class and_the_joined_subject_is_erased(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification(fixture)
    {
        public EventSourceId CaseId { get; } = "multi-subject-case-1";
        public EventSourceId AdvisorId { get; } = "multi-subject-advisor-1";
        public string SubjectName { get; } = "Ada Lovelace";
        public string UpdatedSubjectName { get; } = "Augusta Ada King";
        public string AdvisorName { get; } = "Grace Hopper";

        public MultiSubjectCase BeforeErasure { get; private set; } = default!;
        public MultiSubjectCase AfterErasure { get; private set; } = default!;
        public BsonDocument? StoredDocument { get; private set; }

        public bool DocumentCanBeInspected => StoredReadModelDocument.CanBeInspected(ChronicleFixture);

        public override IEnumerable<Type> EventTypes => [typeof(MultiSubjectCaseOpened), typeof(MultiSubjectCaseRenamed), typeof(MultiSubjectAdvisorNamed)];
        public override IEnumerable<Type> ModelBoundProjections => [typeof(MultiSubjectCase)];

        async Task Because()
        {
            var projectionId = EventStore.Projections.GetProjectionIdForModel<MultiSubjectCase>();
            var handler = EventStore.Projections.GetAllHandlers().Single(_ => _.Id == projectionId);
            await handler.WaitTillActive();

            await EventStore.EventLog.Append(AdvisorId, new MultiSubjectAdvisorNamed(AdvisorName));
            await EventStore.EventLog.Append(CaseId, new MultiSubjectCaseOpened(AdvisorId.Value, SubjectName));
            var caseUpdate = await EventStore.EventLog.Append(CaseId, new MultiSubjectCaseRenamed(UpdatedSubjectName));
            await handler.WaitTillReachesEventSequenceNumber(caseUpdate.SequenceNumber);

            BeforeErasure = await EventStore.ReadModels.GetInstanceById<MultiSubjectCase>(CaseId.Value);
            StoredDocument = await StoredReadModelDocument.Read(ChronicleFixture, "MultiSubjectCases");

            await EventStore.PII.DeleteEncryptionKeyFor(AdvisorId.Value);
            AfterErasure = await EventStore.ReadModels.GetInstanceById<MultiSubjectCase>(CaseId.Value);
        }
    }

    [Fact] void should_release_the_documents_updated_pii_before_erasure() => Context.BeforeErasure.SubjectName.ShouldEqual(Context.UpdatedSubjectName);
    [Fact] void should_release_the_joined_pii_before_erasure() => Context.BeforeErasure.AdvisorName.ShouldEqual(Context.AdvisorName);
    [Fact] void should_have_read_the_stored_document_when_the_backend_allows_it() => (!Context.DocumentCanBeInspected || Context.StoredDocument is not null).ShouldBeTrue();
    [Fact] void should_store_the_documents_own_pii_encrypted() => Context.StoredDocument?["SubjectName"].AsString.ShouldNotEqual(Context.UpdatedSubjectName);
    [Fact] void should_store_the_joined_pii_encrypted() => Context.StoredDocument?["AdvisorName"].AsString.ShouldNotEqual(Context.AdvisorName);
    [Fact]
    void should_store_the_document_subject_when_the_backend_allows_it()
    {
        if (Context.DocumentCanBeInspected)
        {
            Context.StoredDocument!["__subject"].AsString.ShouldEqual(Context.CaseId.Value);
        }
    }

    [Fact]
    void should_store_the_joined_property_subject_when_the_backend_allows_it()
    {
        if (Context.DocumentCanBeInspected)
        {
            Context.StoredDocument!.TryGetValue("__subjects", out var subjects).ShouldBeTrue();
            subjects["AdvisorName"].AsString.ShouldEqual(Context.AdvisorId.Value);
        }
    }
    [Fact] void should_keep_the_documents_own_pii_after_erasing_the_joined_subject() => Context.AfterErasure.SubjectName.ShouldEqual(Context.UpdatedSubjectName);
    [Fact] void should_erase_only_the_joined_subjects_pii() => Context.AfterErasure.AdvisorName.ShouldEqual(string.Empty);
}

/// <summary>
/// A case was opened for a subject and assigned to an advisor.
/// </summary>
/// <param name="AdvisorId">The assigned advisor.</param>
/// <param name="SubjectName">The case subject's name.</param>
[EventType]
public record MultiSubjectCaseOpened(string AdvisorId, [property: PII] string SubjectName);

/// <summary>
/// A case subject's name was changed after the case had been opened.
/// </summary>
/// <param name="SubjectName">The case subject's new name.</param>
[EventType]
public record MultiSubjectCaseRenamed([property: PII] string SubjectName);

/// <summary>
/// An advisor was named on the advisor's own event source.
/// </summary>
/// <param name="FullName">The advisor's name.</param>
[EventType]
public record MultiSubjectAdvisorNamed([property: PII] string FullName);

/// <summary>
/// A stored case composed from PII belonging to the case subject and the assigned advisor.
/// </summary>
/// <param name="Id">The case identifier and default compliance subject.</param>
/// <param name="AdvisorId">The assigned advisor.</param>
/// <param name="SubjectName">The case subject's name.</param>
/// <param name="AdvisorName">The advisor's name.</param>
[FromEvent<MultiSubjectCaseOpened>]
[FromEvent<MultiSubjectCaseRenamed>]
public record MultiSubjectCase(
    string Id,
    string AdvisorId,
    [property: PII] string SubjectName,
    [property: PII]
    [Join<MultiSubjectAdvisorNamed>(on: nameof(AdvisorId), eventPropertyName: nameof(MultiSubjectAdvisorNamed.FullName))]
    string AdvisorName);

#pragma warning restore SA1402
