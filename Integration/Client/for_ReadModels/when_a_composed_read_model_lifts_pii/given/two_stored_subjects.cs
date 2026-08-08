// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Integration.for_ReadModels.when_a_composed_read_model_lifts_pii.given;

/// <summary>
/// Two people, each with a postponement comment and a review note stored encrypted under their own subject
/// by a real kernel, and the ciphertext exactly as it sits at rest.
/// </summary>
/// <remarks>
/// The ciphertext is the whole point of running these at this tier. A query method that composes a row in
/// memory from a collection it read itself gets these bytes, not the plaintext the kernel hands back through
/// <c>GetInstanceById</c> — and an in-memory harness that substitutes the source collection never produces
/// them at all, so no spec below this tier can tell a released value from an unreleased one.
/// </remarks>
/// <param name="fixture">The <see cref="ChronicleFixture"/> for the run.</param>
public class two_stored_subjects(ChronicleFixture fixture) : Specification(fixture)
{
    const string CollectionName = "RetentionSubjects";

    /// <summary>
    /// Gets the first person's identity.
    /// </summary>
    public EventSourceId FirstPerson { get; } = "composed-pii-person-1";

    /// <summary>
    /// Gets the second person's identity.
    /// </summary>
    public EventSourceId SecondPerson { get; } = "composed-pii-person-2";

    /// <summary>
    /// Gets the event stored for the first person.
    /// </summary>
    public RetentionPostponed FirstEvent { get; } = new("Awaiting counsel from the family", new ReviewNote("Spoke to the next of kin", "advisor-3"));

    /// <summary>
    /// Gets the event stored for the second person.
    /// </summary>
    public RetentionPostponed SecondEvent { get; } = new("Contract dispute is still open", new ReviewNote("Legal is preparing a summary", "advisor-7"));

    /// <summary>
    /// Gets the first person's comment as it sits at rest.
    /// </summary>
    public string FirstCommentAtRest { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the second person's comment as it sits at rest.
    /// </summary>
    public string SecondCommentAtRest { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the first person's note text as it sits at rest.
    /// </summary>
    public string FirstNoteTextAtRest { get; private set; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether the run stores read models where they can be read back at rest.
    /// </summary>
    public bool DocumentsCanBeInspected => StoredReadModelDocument.CanBeInspected(ChronicleFixture);

    /// <inheritdoc/>
    public override IEnumerable<Type> EventTypes => [typeof(RetentionPostponed)];

    /// <inheritdoc/>
    public override IEnumerable<Type> ModelBoundProjections => [typeof(RetentionSubject)];

    /// <summary>
    /// Append both people's events, wait for the kernel to store them, and read back what it wrote.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    protected async Task StoreBothSubjects()
    {
        await EventStore.EventLog.Append(FirstPerson, FirstEvent);
        await EventStore.EventLog.Append(SecondPerson, SecondEvent);

        await WaitUntilStored(FirstPerson);
        await WaitUntilStored(SecondPerson);

        if (!DocumentsCanBeInspected)
        {
            return;
        }

        var documents = await ChronicleFixture.ReadModels.Database
            .GetCollection<BsonDocument>(CollectionName)
            .Find(Builders<BsonDocument>.Filter.Empty)
            .ToListAsync();

        var first = documents.Single(document => document["_id"].AsString == FirstPerson.Value);
        var second = documents.Single(document => document["_id"].AsString == SecondPerson.Value);

        FirstCommentAtRest = first["Comment"].AsString;
        SecondCommentAtRest = second["Comment"].AsString;
        FirstNoteTextAtRest = first["Note"]["Text"].AsString;
    }

    async Task WaitUntilStored(EventSourceId person)
    {
        using var cts = new CancellationTokenSource(TimeSpanFactory.DefaultTimeout());
        while (true)
        {
            var instance = await EventStore.ReadModels.GetInstanceById<RetentionSubject>(person.Value);
            if (instance?.Comment is not null && instance.Note?.Text is not null)
            {
                return;
            }

            await Task.Delay(200, cts.Token);
        }
    }
}
