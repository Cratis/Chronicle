// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Occurrences;

/// <summary>
/// Stores infrastructure occurrences directly in a namespace or application database.
/// </summary>
/// <param name="database">The scoped database.</param>
public partial class OccurrencesStorage(IMongoDatabase database) : IOccurrencesStorage, IScheduleStorage
{
    readonly IMongoCollection<BsonDocument> _occurrences = database.GetCollection<BsonDocument>("occurrences");
    readonly IMongoCollection<BsonDocument> _fences = database.GetCollection<BsonDocument>("occurrence-claim-fences");

    /// <inheritdoc/>
    public async Task<OccurrenceRecordResult> Record(Occurrence occurrence)
    {
        try
        {
            await _occurrences.InsertOneAsync(OccurrenceDocuments.From(occurrence));
            return OccurrenceRecordResult.Recorded;
        }
        catch (MongoWriteException error) when (error.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            var existing = await Get(occurrence.Id);
            if (existing is null)
            {
                throw;
            }

            return existing.Fingerprint == occurrence.Fingerprint ? OccurrenceRecordResult.AlreadyRecorded : OccurrenceRecordResult.OccurrenceIdConflict;
        }
    }

    /// <inheritdoc/>
    public async Task<Occurrence?> Get(OccurrenceId id)
    {
        var document = await _occurrences.Find(Id(id.Value)).SingleOrDefaultAsync();
        return document is null ? null : OccurrenceDocuments.ToOccurrence(document);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Occurrence>> GetForTrigger(TriggerId trigger)
    {
        var documents = await _occurrences.Find(new BsonDocument("trigger", trigger.Value)).ToListAsync();
        return documents.Select(OccurrenceDocuments.ToOccurrence).ToArray();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Occurrence>> Claim(TriggerId trigger, string worker, DateTimeOffset now, TimeSpan duration, int max, int maxInFlight)
    {
        await EnsureFence(trigger);
        using var session = await database.Client.StartSessionAsync();
        try
        {
            session.StartTransaction();

            // Avoid snapshot write skew when two workers claim different pending rows.
            await _fences.UpdateOneAsync(session, Id(trigger.Value), Builders<BsonDocument>.Update.Inc("version", 1L));
            var documents = await _occurrences.Find(session, new BsonDocument("trigger", trigger.Value)).ToListAsync();
            var occurrences = documents.Select(OccurrenceDocuments.ToOccurrence).ToArray();
            var candidates = OccurrenceClaimSelection.Select(occurrences, now, max, maxInFlight);
            var claimed = new List<Occurrence>();
            foreach (var occurrence in candidates)
            {
                var next = occurrence with { State = OccurrenceState.Leased, Epoch = occurrence.Epoch + 1, Attempts = occurrence.Attempts + 1, Owner = worker, LeaseExpiry = now.Add(duration), NextAttempt = null };
                var update = Builders<BsonDocument>.Update.Set("state", (int)next.State).Set("epoch", next.Epoch).Set("attempts", next.Attempts)
                    .Set("owner", worker).Set("expiry", next.LeaseExpiry.Value.UtcTicks).Set("nextAttempt", BsonNull.Value);
                await _occurrences.UpdateOneAsync(session, Id(occurrence.Id.Value) & Builders<BsonDocument>.Filter.Eq("epoch", occurrence.Epoch), update);
                claimed.Add(next);
            }

            await session.CommitTransactionAsync();
            return claimed.ToArray();
        }
        catch (MongoException error) when (error.HasErrorLabel("TransientTransactionError"))
        {
            await Abort(session);
            return [];
        }
        catch
        {
            await Abort(session);
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<OccurrenceLeaseResult> Renew(LeaseToken token, DateTimeOffset expiry) => Transition(token, Builders<BsonDocument>.Update.Set("expiry", expiry.UtcTicks));

    /// <inheritdoc/>
    public async Task<OccurrenceLeaseResult> Complete(LeaseToken token)
    {
        var result = await Transition(token, Builders<BsonDocument>.Update.Set("state", (int)OccurrenceState.Completed).Set("owner", BsonNull.Value).Set("expiry", BsonNull.Value));
        if (result == OccurrenceLeaseResult.Updated)
        {
            return result;
        }

        var existing = await Get(token.OccurrenceId);
        return existing?.State == OccurrenceState.Completed && existing.Epoch == token.Epoch ? OccurrenceLeaseResult.AlreadyCompleted : OccurrenceLeaseResult.OccurrenceLeaseLost;
    }

    /// <inheritdoc/>
    public Task<OccurrenceLeaseResult> Fail(LeaseToken token, string error, DateTimeOffset? nextAttempt) => Transition(
        token,
        Builders<BsonDocument>.Update.Set("state", (int)(nextAttempt.HasValue ? OccurrenceState.Pending : OccurrenceState.Quarantined))
            .Set("owner", BsonNull.Value).Set("expiry", BsonNull.Value).Set("lastError", error).Set("nextAttempt", OccurrenceDocuments.Ticks(nextAttempt)));

    /// <inheritdoc/>
    public async Task<bool> RetryQuarantined(OccurrenceId id)
    {
        var result = await _occurrences.UpdateOneAsync(
            Id(id.Value) & Builders<BsonDocument>.Filter.Eq("state", (int)OccurrenceState.Quarantined),
            Builders<BsonDocument>.Update.Set("state", (int)OccurrenceState.Pending).Set("attempts", 0).Set("nextAttempt", BsonNull.Value));
        return result.ModifiedCount == 1;
    }

    static FilterDefinition<BsonDocument> Id(string id) => Builders<BsonDocument>.Filter.Eq("_id", id);

    static async Task Abort(IClientSessionHandle session)
    {
        if (session.IsInTransaction)
        {
            await session.AbortTransactionAsync();
        }
    }

    async Task<OccurrenceLeaseResult> Transition(LeaseToken token, UpdateDefinition<BsonDocument> update)
    {
        var filter = Id(token.OccurrenceId.Value) & Builders<BsonDocument>.Filter.Eq("epoch", token.Epoch) & Builders<BsonDocument>.Filter.Eq("state", (int)OccurrenceState.Leased);
        var result = await _occurrences.UpdateOneAsync(filter, update);
        return result.MatchedCount == 1 ? OccurrenceLeaseResult.Updated : OccurrenceLeaseResult.OccurrenceLeaseLost;
    }

    async Task EnsureFence(TriggerId trigger)
    {
        try
        {
            await _fences.UpdateOneAsync(Id(trigger.Value), Builders<BsonDocument>.Update.SetOnInsert("version", 0L), new UpdateOptions { IsUpsert = true });
        }
        catch (MongoWriteException error) when (error.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            if (!await _fences.Find(Id(trigger.Value)).AnyAsync())
            {
                throw;
            }
        }
    }
}
