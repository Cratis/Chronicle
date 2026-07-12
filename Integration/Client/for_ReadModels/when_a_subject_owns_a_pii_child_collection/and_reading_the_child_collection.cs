// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402

using System.Text.Json;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;
using context = Cratis.Chronicle.Integration.for_ReadModels.when_a_subject_owns_a_pii_child_collection.and_reading_the_child_collection.context;

namespace Cratis.Chronicle.Integration.for_ReadModels.when_a_subject_owns_a_pii_child_collection;

/// <summary>
/// End-to-end regression for #3463. A subject owns two read models projected in the same run: a root-PII
/// model (a simple fluent projection) and a sibling <b>non-passive, model-bound <c>[ChildrenFrom]</c></b>
/// model whose children carry <c>[PII]</c>. Both are keyed by — and encrypt under — the same subject
/// (the event source id). The child collection reliably fails to materialize in the out-of-process kernel
/// (the poll on <c>Assessments.Count</c> times out) even though the parent document is returned and the
/// simple sibling projection materializes fine in the exact same run.
/// </summary>
/// <param name="context">The test context.</param>
[Collection(ChronicleCollection.Name)]
public class and_reading_the_child_collection(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification(fixture)
    {
        public EventSourceId ApplicantId { get; } = "applicant-1";
        public ApplicantRegistered RegisteredEvent { get; private set; } = default!;
        public IReadOnlyList<AssessmentRecorded> AssessmentEvents { get; private set; } = default!;

        public ApplicantProfile SiblingModel { get; private set; } = default!;
        public ApplicantDossier Dossier { get; private set; } = default!;

        public override IEnumerable<Type> EventTypes => [typeof(ApplicantRegistered), typeof(AssessmentRecorded)];

        public override IEnumerable<Type> Projections => [typeof(ApplicantProfileProjection)];

        public override IEnumerable<Type> ModelBoundProjections => [typeof(ApplicantDossier)];

        void Establish()
        {
            RegisteredEvent = new ApplicantRegistered("Ada Lovelace", "Prefers to be contacted by letter");
            AssessmentEvents =
            [
                new AssessmentRecorded(Guid.Parse("8b95cb59-3a99-4bda-a11a-cd0000000201"), "communication", "Wrote a warm, detailed cover letter"),
                new AssessmentRecorded(Guid.Parse("8b95cb59-3a99-4bda-a11a-cd0000000202"), "experience", "Ten years building analytical engines"),
                new AssessmentRecorded(Guid.Parse("8b95cb59-3a99-4bda-a11a-cd0000000203"), "culture", "Would mentor juniors on her own time")
            ];
        }

        async Task Because()
        {
            await EventStore.EventLog.Append(ApplicantId, RegisteredEvent);
            foreach (var assessment in AssessmentEvents)
            {
                await EventStore.EventLog.Append(ApplicantId, assessment);
            }

            // The simple sibling projection is expected to materialize; wait for it first so the
            // child-collection poll below is not gated on the subject's other read model.
            SiblingModel = await PollUntil<ApplicantProfile>(_ => _.FullName == RegisteredEvent.FullName);
            Dossier = await PollUntil<ApplicantDossier>(_ => _.Assessments is { Count: 3 });
        }

        async Task<TReadModel> PollUntil<TReadModel>(Func<TReadModel, bool> ready)
            where TReadModel : class
        {
            using var cts = new CancellationTokenSource(TimeSpanFactory.DefaultTimeout());
            TReadModel? last = null;
            try
            {
                while (true)
                {
                    last = await EventStore.ReadModels.GetInstanceById<TReadModel>(ApplicantId.Value);
                    if (last is not null && ready(last))
                    {
                        return last;
                    }

                    await Task.Delay(200, cts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                // Surface kernel-side diagnostics into the CI job log — the container logs (at Debug level
                // for Cratis) carry the observer subscription / routing traces that explain why the child
                // collection never materialized. Without this a CI-only timeout is opaque.
                var lastJson = last is null ? "null" : JsonSerializer.Serialize(last);
                var kernelLogs = ChronicleFixture is ChronicleConfigurableFixture configurable
                    ? await configurable.GetOutOfProcessKernelLogs()
                    : "(fixture is not configurable; no kernel logs)";
                throw new TimeoutException(
                    $"'{typeof(TReadModel).Name}' never satisfied the readiness condition for '{ApplicantId.Value}' within the poll timeout.\n" +
                    $"Last read model instance: {lastJson}\n\n{kernelLogs}");
            }
        }
    }

    [Fact] void should_materialize_the_sibling_model() => Context.SiblingModel.ShouldNotBeNull();
    [Fact] void should_return_the_dossier() => Context.Dossier.ShouldNotBeNull();
    [Fact] void should_have_three_assessments() => Context.Dossier.Assessments.Count.ShouldEqual(3);
    [Fact] void should_keep_each_assessment_identifier() => Context.Dossier.Assessments.Select(_ => _.AssessmentId).Order().ShouldEqual(Context.AssessmentEvents.Select(_ => _.AssessmentId).Order());
    [Fact] void should_release_child_pii_to_plaintext() => Context.Dossier.Assessments.Select(_ => _.Note).Order().ShouldEqual(Context.AssessmentEvents.Select(_ => _.Note).Order());
}

[EventType]
public record ApplicantRegistered(string FullName, [property: PII] string PersonalNote);

[EventType]
public record AssessmentRecorded(Guid AssessmentId, string Criterion, [property: PII] string Note);

public record ApplicantProfile(string FullName, [property: PII] string PersonalNote);

[FromEvent<ApplicantRegistered>]
public record ApplicantDossier(
    string FullName,
    [ChildrenFrom<AssessmentRecorded>(key: nameof(AssessmentRecorded.AssessmentId), identifiedBy: nameof(DossierAssessment.AssessmentId))]
    IReadOnlyList<DossierAssessment> Assessments);

[FromEvent<AssessmentRecorded>]
public record DossierAssessment(
    [Key] Guid AssessmentId,
    string Criterion,
    [property: PII] string Note);

public class ApplicantProfileProjection : IProjectionFor<ApplicantProfile>
{
    public ProjectionId Identifier => "pii-child-collection-applicant-profile";

    public void Define(IProjectionBuilderFor<ApplicantProfile> builder) => builder
        .From<ApplicantRegistered>(e => e
            .Set(m => m.FullName).To(e => e.FullName)
            .Set(m => m.PersonalNote).To(e => e.PersonalNote));
}

#pragma warning restore SA1402
