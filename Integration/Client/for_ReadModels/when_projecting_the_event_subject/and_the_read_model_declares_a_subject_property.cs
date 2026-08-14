// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402

using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;
using MongoDB.Bson;
using context = Cratis.Chronicle.Integration.for_ReadModels.when_projecting_the_event_subject.and_the_read_model_declares_a_subject_property.context;

namespace Cratis.Chronicle.Integration.for_ReadModels.when_projecting_the_event_subject;

/// <summary>
/// Verifies that a subject explicitly carried by event context can also be projected into an ordinary
/// read-model property while Chronicle retains its internal per-property ownership metadata.
/// </summary>
/// <param name="context">The test context.</param>
[Collection(ChronicleCollection.Name)]
public class and_the_read_model_declares_a_subject_property(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification(fixture)
    {
        public EventSourceId SignupId { get; } = "subject-property-signup";
        public Subject UserId { get; } = "subject-property-user";
        public string Email { get; } = "user@example.com";

        public SubjectSignup Result { get; private set; } = default!;
        public BsonDocument? StoredDocument { get; private set; }

        public bool DocumentCanBeInspected => StoredReadModelDocument.CanBeInspected(ChronicleFixture);

        public override IEnumerable<Type> EventTypes => [typeof(SubjectSignupRegistered)];
        public override IEnumerable<Type> ModelBoundProjections => [typeof(SubjectSignup)];

        async Task Because()
        {
            var projectionId = EventStore.Projections.GetProjectionIdForModel<SubjectSignup>();
            var handler = EventStore.Projections.GetAllHandlers().Single(_ => _.Id == projectionId);
            await handler.WaitTillActive();

            var appendResult = await EventStore.EventLog.Append(SignupId, new SubjectSignupRegistered(Email), subject: UserId);
            await handler.WaitTillReachesEventSequenceNumber(appendResult.SequenceNumber);

            Result = await EventStore.ReadModels.GetInstanceById<SubjectSignup>(SignupId.Value);
            StoredDocument = await StoredReadModelDocument.Read(ChronicleFixture, "SubjectSignups");
        }
    }

    [Fact] void should_return_the_read_model() => Context.Result.ShouldNotBeNull();
    [Fact] void should_materialize_the_event_subject() => Context.Result.Subject.ShouldEqual(Context.UserId);
    [Fact] void should_release_the_pii_value() => Context.Result.Email.ShouldEqual(Context.Email);
    [Fact] void should_have_read_the_stored_document_when_the_backend_allows_it() => (!Context.DocumentCanBeInspected || Context.StoredDocument is not null).ShouldBeTrue();
    [Fact]
    void should_store_the_projected_subject_property_when_the_backend_allows_it()
    {
        if (Context.DocumentCanBeInspected)
        {
            Context.StoredDocument![nameof(SubjectSignup.Subject)].AsString.ShouldEqual(Context.UserId.Value);
        }
    }
}

/// <summary>
/// A signup was registered for a user.
/// </summary>
/// <param name="Email">The user's email address.</param>
[EventType]
public record SubjectSignupRegistered([property: PII] string Email);

/// <summary>
/// A signup carrying the user that owns its personal data.
/// </summary>
/// <param name="Id">The signup identifier.</param>
/// <param name="Subject">The user that owns the personal data.</param>
/// <param name="Email">The user's email address.</param>
[FromEvent<SubjectSignupRegistered>]
public record SubjectSignup(
    string Id,
    [property: Subject]
    [SetFromContext<SubjectSignupRegistered>(nameof(EventContext.Subject))]
    Subject Subject,
    string Email);

#pragma warning restore SA1402
