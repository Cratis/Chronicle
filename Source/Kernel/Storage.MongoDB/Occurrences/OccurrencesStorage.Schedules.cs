// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Storage.Occurrences;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Occurrences;

public partial class OccurrencesStorage
{
    readonly IMongoCollection<BsonDocument> _definitions = database.GetCollection<BsonDocument>("schedule-definitions");
    readonly IMongoCollection<BsonDocument> _cursors = database.GetCollection<BsonDocument>("schedule-cursors");
    readonly IMongoCollection<BsonDocument> _skipped = database.GetCollection<BsonDocument>("skipped-ranges");

    /// <inheritdoc/>
    public async Task<ScheduleDefinition?> GetDefinition(ScheduleToken token)
    {
        var document = await _definitions.Find(Id(token.Value)).SingleOrDefaultAsync();
        return document is null ? null : JsonSerializer.Deserialize<ScheduleDefinition>(document["data"].AsString);
    }

    /// <inheritdoc/>
    public Task<bool> TryAddDefinition(ScheduleDefinition definition) => TryInsert(_definitions, DefinitionDocument(definition));

    /// <inheritdoc/>
    public async Task<bool> TryUpdateDefinition(long expectedRevision, ScheduleDefinition definition)
    {
        var result = await _definitions.ReplaceOneAsync(Id(definition.Token.Value) & Builders<BsonDocument>.Filter.Eq("revision", expectedRevision), DefinitionDocument(definition));
        return result.MatchedCount == 1;
    }

    /// <inheritdoc/>
    public async Task<ScheduleCursor?> GetCursor(ScheduleToken token)
    {
        var document = await _cursors.Find(Id(token.Value)).SingleOrDefaultAsync();
        return document is null ? null : Cursor(document);
    }

    /// <inheritdoc/>
    public Task<bool> TryAddCursor(ScheduleCursor cursor) => TryInsert(_cursors, CursorDocument(cursor));

    /// <inheritdoc/>
    public async Task RegisterRevision(ScheduleToken token, long revision) =>
        await _cursors.UpdateOneAsync(Id(token.Value) & Builders<BsonDocument>.Filter.Lt("registered", revision), Builders<BsonDocument>.Update.Set("registered", revision).Inc("version", 1L));

    /// <inheritdoc/>
    public async Task<bool> TryCommitTurn(ScheduleTurn turn)
    {
        if (turn.Next.Token != turn.Observed.Token || turn.Next.Version != turn.Observed.Version + 1 || turn.Next.RegisteredRevision != turn.Observed.RegisteredRevision || turn.Next.AppliedRevision != turn.Observed.RegisteredRevision)
        {
            return false;
        }

        await EnsureIndexes();
        using var session = await database.Client.StartSessionAsync();
        try
        {
            session.StartTransaction();
            var filter = Id(turn.Observed.Token.Value) & Builders<BsonDocument>.Filter.Eq("version", turn.Observed.Version) & Builders<BsonDocument>.Filter.Eq("registered", turn.Observed.RegisteredRevision);
            var result = await _cursors.ReplaceOneAsync(session, filter, CursorDocument(turn.Next));
            if (result.MatchedCount != 1)
            {
                await Abort(session);
                return false;
            }

            foreach (var occurrence in turn.Occurrences)
            {
                var document = OccurrenceDocuments.From(occurrence);
                document.Add("schedule", turn.Observed.Token.Value);
                await _occurrences.UpdateOneAsync(session, Id(occurrence.Id.Value), new BsonDocument("$setOnInsert", document), new UpdateOptions { IsUpsert = true });
            }

            foreach (var range in turn.SkippedRanges)
            {
                var document = new BsonDocument { { "_id", range.Id }, { "token", range.Token.Value }, { "data", JsonSerializer.Serialize(range) } };
                await _skipped.UpdateOneAsync(session, Id(range.Id), new BsonDocument("$setOnInsert", document), new UpdateOptions { IsUpsert = true });
            }

            if (turn.DiscardPendingBeforeRevision.HasValue)
            {
                var pending = await _occurrences.Find(session, new BsonDocument { { "schedule", turn.Observed.Token.Value }, { "state", (int)OccurrenceState.Pending } }).ToListAsync();
                foreach (var document in pending.Where(document => OccurrenceDocuments.ToOccurrence(document).DefinitionRevision < turn.DiscardPendingBeforeRevision))
                {
                    await _occurrences.UpdateOneAsync(session, Id(document["_id"].AsString), Builders<BsonDocument>.Update.Set("state", (int)OccurrenceState.Skipped));
                }
            }

            await session.CommitTransactionAsync();
            return true;
        }
        catch (MongoException error) when (error.HasErrorLabel("TransientTransactionError"))
        {
            await Abort(session);
            return false;
        }
        catch
        {
            await Abort(session);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<SkippedRange>> GetSkippedRanges(ScheduleToken token)
    {
        var documents = await _skipped.Find(new BsonDocument("token", token.Value)).ToListAsync();
        return documents.Select(document => JsonSerializer.Deserialize<SkippedRange>(document["data"].AsString)!).ToArray();
    }

    static BsonDocument DefinitionDocument(ScheduleDefinition definition) => new() { { "_id", definition.Token.Value }, { "revision", definition.Revision }, { "data", JsonSerializer.Serialize(definition) } };

    static BsonDocument CursorDocument(ScheduleCursor cursor) => new()
    {
        { "_id", cursor.Token.Value }, { "due", cursor.LastMaterializedDue }, { "registered", cursor.RegisteredRevision }, { "applied", cursor.AppliedRevision }, { "version", cursor.Version }
    };

    static ScheduleCursor Cursor(BsonDocument document) => new(document["_id"].AsString, document["due"].ToInt64(), document["registered"].ToInt64(), document["applied"].ToInt64(), document["version"].ToInt64());

    static async Task<bool> TryInsert(IMongoCollection<BsonDocument> collection, BsonDocument document)
    {
        try
        {
            await collection.InsertOneAsync(document);
            return true;
        }
        catch (MongoWriteException error) when (error.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return false;
        }
    }
}
