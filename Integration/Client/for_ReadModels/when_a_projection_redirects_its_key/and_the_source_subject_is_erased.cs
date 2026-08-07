// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402

using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Events;
using MongoDB.Bson;
using context = Cratis.Chronicle.Integration.for_ReadModels.when_a_projection_redirects_its_key.and_the_source_subject_is_erased.context;

namespace Cratis.Chronicle.Integration.for_ReadModels.when_a_projection_redirects_its_key;

/// <summary>
/// Measures what key redirection does to erasure reach. One event carrying one person's <c>[PII]</c> is
/// appended to that person's own stream and projected twice in the same run: once with the default key
/// (the event source id) and once with <c>UsingKey</c> pointing the document at an unrelated request id.
/// The person's encryption key is then deleted — the client-side right-to-erasure seam — and both read
/// models are read again.
/// </summary>
/// <remarks>
/// The redirected document is stamped with the request id as its compliance subject, so the advisor's name
/// is re-encrypted under the request rather than under the advisor. Deleting the advisor's key therefore
/// blanks the control document and leaves the redirected copy fully readable. No error is raised on any
/// path — the copy reads back cleanly before and after the erasure, which is exactly what makes the defect
/// invisible without a rule.
/// </remarks>
/// <param name="context">The test context.</param>
[Collection(ChronicleCollection.Name)]
public class and_the_source_subject_is_erased(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification(fixture)
    {
        public EventSourceId AdvisorId { get; } = "key-redirection-advisor-1";
        public string RequestId { get; } = "key-redirection-request-1";
        public AdvisorNamed Event { get; private set; } = default!;

        public AdvisorOnOwnStream? OnOwnStreamBeforeErasure { get; private set; }
        public AdvisorOnRequest? OnRequestBeforeErasure { get; private set; }
        public AdvisorOnOwnStream? OnOwnStreamAfterErasure { get; private set; }
        public AdvisorOnRequest? OnRequestAfterErasure { get; private set; }
        public BsonDocument? StoredRedirectedDocument { get; private set; }

        public bool DocumentCanBeInspected => StoredReadModelDocument.CanBeInspected(ChronicleFixture);

        const string RedirectedContainerName = "AdvisorOnRequests";

        public override IEnumerable<Type> EventTypes => [typeof(AdvisorNamed)];

        public override IEnumerable<Type> Projections => [typeof(AdvisorOnOwnStreamProjection), typeof(AdvisorOnRequestProjection)];

        void Establish() => Event = new AdvisorNamed(RequestId, "Ada Lovelace");

        async Task Because()
        {
            await EventStore.EventLog.Append(AdvisorId, Event);

            OnOwnStreamBeforeErasure = await PollUntil<AdvisorOnOwnStream>(AdvisorId.Value, _ => _.FullName == Event.FullName);
            OnRequestBeforeErasure = await PollUntil<AdvisorOnRequest>(RequestId, _ => _.AdvisorName == Event.FullName);

            StoredRedirectedDocument = await StoredReadModelDocument.Read(ChronicleFixture, RedirectedContainerName);

            // The advisor exercises the right to erasure: crypto-shred the key held under their subject.
            await EventStore.PII.DeleteEncryptionKeyFor(AdvisorId.Value);

            OnOwnStreamAfterErasure = await EventStore.ReadModels.GetInstanceById<AdvisorOnOwnStream>(AdvisorId.Value);
            OnRequestAfterErasure = await EventStore.ReadModels.GetInstanceById<AdvisorOnRequest>(RequestId);
        }

        async Task<TReadModel> PollUntil<TReadModel>(string key, Func<TReadModel, bool> ready)
            where TReadModel : class
        {
            using var cts = new CancellationTokenSource(TimeSpanFactory.DefaultTimeout());
            while (true)
            {
                var instance = await EventStore.ReadModels.GetInstanceById<TReadModel>(key);
                if (instance is not null && ready(instance))
                {
                    return instance;
                }

                await Task.Delay(200, cts.Token);
            }
        }
    }

    [Fact] void should_release_the_name_on_the_advisors_own_read_model_before_erasure() => Context.OnOwnStreamBeforeErasure!.FullName.ShouldEqual(Context.Event.FullName);
    [Fact] void should_release_the_name_on_the_redirected_read_model_before_erasure() => Context.OnRequestBeforeErasure!.AdvisorName.ShouldEqual(Context.Event.FullName);
    [Fact] void should_have_read_the_stored_document_when_the_backend_allows_it() => (!Context.DocumentCanBeInspected || Context.StoredRedirectedDocument is not null).ShouldBeTrue();
    [Fact] void should_stamp_the_redirected_document_with_the_redirected_key_as_its_subject() => Context.StoredRedirectedDocument?["__subject"].AsString.ShouldEqual(Context.RequestId);
    [Fact] void should_not_stamp_the_redirected_document_with_the_advisors_subject() => Context.StoredRedirectedDocument?["__subject"].AsString.ShouldNotEqual(Context.AdvisorId.Value);
    [Fact] void should_blank_the_name_on_the_advisors_own_read_model_after_erasure() => Context.OnOwnStreamAfterErasure!.FullName.ShouldEqual(string.Empty);
    [Fact] void should_leave_the_redirected_copy_readable_after_erasure() => Context.OnRequestAfterErasure!.AdvisorName.ShouldEqual(Context.Event.FullName);
}

/// <summary>
/// The advisor's name is personal data belonging to the advisor, and the event carries no <c>[Subject]</c>,
/// so its compliance subject is the stream it is appended to — the advisor's.
/// </summary>
/// <param name="RequestId">The unrelated request the advisor acted on.</param>
/// <param name="FullName">The advisor's name.</param>
[EventType]
public record AdvisorNamed(string RequestId, [property: PII] string FullName);

/// <summary>
/// The control: keyed by the event source id, so the document's compliance subject is the advisor's own.
/// </summary>
/// <param name="Id">The advisor's identifier.</param>
/// <param name="FullName">The advisor's name.</param>
public record AdvisorOnOwnStream(string Id, [property: PII] string FullName);

/// <summary>
/// The redirected case: <c>UsingKey</c> points the document at the request, so the document's compliance
/// subject is the request rather than the advisor.
/// </summary>
/// <param name="Id">The request identifier.</param>
/// <param name="AdvisorName">The advisor's name, resting on a document subjected to the request.</param>
public record AdvisorOnRequest(string Id, [property: PII] string AdvisorName);

/// <summary>
/// Projects <see cref="AdvisorNamed"/> onto the advisor's own stream key.
/// </summary>
public class AdvisorOnOwnStreamProjection : IProjectionFor<AdvisorOnOwnStream>
{
    /// <summary>
    /// Gets the projection identifier.
    /// </summary>
    public ProjectionId Identifier => "key-redirection-advisor-on-own-stream";

    /// <summary>
    /// Defines the projection.
    /// </summary>
    /// <param name="builder">The projection builder.</param>
    public void Define(IProjectionBuilderFor<AdvisorOnOwnStream> builder) => builder
        .From<AdvisorNamed>(_ => _
            .Set(m => m.FullName).To(e => e.FullName));
}

/// <summary>
/// Projects the same <see cref="AdvisorNamed"/> onto the request's key instead.
/// </summary>
public class AdvisorOnRequestProjection : IProjectionFor<AdvisorOnRequest>
{
    /// <summary>
    /// Gets the projection identifier.
    /// </summary>
    public ProjectionId Identifier => "key-redirection-advisor-on-request";

    /// <summary>
    /// Defines the projection.
    /// </summary>
    /// <param name="builder">The projection builder.</param>
    public void Define(IProjectionBuilderFor<AdvisorOnRequest> builder) => builder
        .From<AdvisorNamed>(_ => _
            .UsingKey(e => e.RequestId)
            .Set(m => m.AdvisorName).To(e => e.FullName));
}

#pragma warning restore SA1402
