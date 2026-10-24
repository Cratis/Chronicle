// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.Events.Constraints;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints;

/// <summary>
/// Represents an implementation of <see cref="IUniqueConstraintsStorage"/>.
/// </summary>
/// <param name="eventStoreNamespaceDatabase"><see cref="IEventStoreNamespaceDatabase"/> for the storage.</param>
/// <param name="eventSequenceId"><see cref="EventSequenceId"/> for the storage.</param>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
public class UniqueConstraintsStorage(
    IEventStoreNamespaceDatabase eventStoreNamespaceDatabase,
    EventSequenceId eventSequenceId,
    ILogger<UniqueConstraintsStorage> logger) : IUniqueConstraintsStorage
{
    const string ValueIndexName = "value";
    const string SubjectIndexName = "subject";
    readonly ConcurrentDictionary<string, byte> _ensuredIndexes = new();

    /// <inheritdoc/>
    public async Task ClearValues(UniqueConstraintDefinition definition, string scopeKey = "", CancellationToken cancellationToken = default)
    {
        if (definition.Mode == UniqueConstraintMode.PerValue)
        {
            await GetValuesCollectionFor(definition.Name, scopeKey).DeleteManyAsync(Builders<UniqueConstraintValueIndex>.Filter.Empty, cancellationToken);
        }
        else
        {
            await GetCollectionFor(definition.Name, scopeKey).DeleteManyAsync(Builders<UniqueConstraintIndex>.Filter.Empty, cancellationToken);
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<UniqueConstraintValue>> GetFor(EventSourceId eventSourceId, UniqueConstraintDefinition definition, string scopeKey = "", CancellationToken cancellationToken = default)
    {
        if (definition.Mode == UniqueConstraintMode.PerValue)
        {
            using var cursor = await GetValuesCollectionFor(definition.Name, scopeKey).FindAsync(row => row.EventSourceId == eventSourceId, cancellationToken: cancellationToken);
            return (await cursor.ToListAsync(cancellationToken)).Select(row => row.Value).ToArray();
        }

        using var sources = await GetCollectionFor(definition.Name, scopeKey).FindAsync(row => row.EventSourceId == eventSourceId, cancellationToken: cancellationToken);
        return (await sources.ToListAsync(cancellationToken)).Select(row => row.Value).ToArray();
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<UniqueConstraintValue> GetForStream(EventSourceId eventSourceId, UniqueConstraintDefinition definition, string scopeKey = "", [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var values = definition.Mode == UniqueConstraintMode.PerValue
            ? StreamPerValue(eventSourceId, definition, scopeKey, cancellationToken)
            : StreamPerEventSource(eventSourceId, definition, scopeKey, cancellationToken);
        await foreach (var value in values.WithCancellation(cancellationToken))
        {
            yield return value;
        }
    }

    /// <inheritdoc/>
    public async Task<(bool IsAllowed, EventSequenceNumber SequenceNumber)> IsAllowed(EventSourceId eventSourceId, UniqueConstraintDefinition definition, UniqueConstraintValue value, string scopeKey = "", CancellationToken cancellationToken = default)
    {
        if (definition.Mode == UniqueConstraintMode.PerValue)
        {
            var values = GetValuesCollectionFor(definition.Name, scopeKey);
            await EnsureOwnerIndex(values, cancellationToken);
            using var found = await values.FindAsync(_ => _.Value == value, cancellationToken: cancellationToken);
            var entry = await found.FirstOrDefaultAsync(cancellationToken);
            return entry is null ? (true, EventSequenceNumber.Unavailable) : (entry.EventSourceId == eventSourceId, entry.SequenceNumber);
        }

        var collection = GetCollectionFor(definition.Name, scopeKey);
        await EnsureIndex(collection, cancellationToken).ConfigureAwait(false);

        // Note: Case-insensitive comparison is now handled by hashing the value with case normalization
        // before it reaches the storage layer, so we can use a simple equality check here.
        using var result = await collection.FindAsync(_ => _.Value == value, cancellationToken: cancellationToken);
        var existing = await result.FirstOrDefaultAsync(cancellationToken);
        if (existing is not null)
        {
            if (existing.EventSourceId == eventSourceId) return (true, existing.SequenceNumber);

            return (false, existing.SequenceNumber);
        }

        return (true, EventSequenceNumber.Unavailable);
    }

    /// <summary>
    /// Saves a claim using the definition's retention mode.
    /// </summary>
    /// <param name="eventSourceId">The owner.</param>
    /// <param name="definition">The constraint.</param>
    /// <param name="sequenceNumber">The claim's sequence number.</param>
    /// <param name="value">The hashed value.</param>
    /// <param name="scopeKey">The resolved scope.</param>
    /// <param name="subject">The subject whose personal data the value derives from, or null.</param>
    /// <param name="cancellationToken">Cancellation of the write.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="DuplicateUniqueConstraintValue">Another event source owns the value.</exception>
    public async Task Save(EventSourceId eventSourceId, UniqueConstraintDefinition definition, EventSequenceNumber sequenceNumber, UniqueConstraintValue value, string scopeKey = "", Subject? subject = default, CancellationToken cancellationToken = default)
    {
        if (definition.Mode != UniqueConstraintMode.PerValue)
        {
            await SavePerEventSource(eventSourceId, definition.Name, sequenceNumber, value, scopeKey, subject, cancellationToken);
            return;
        }

        var collection = GetValuesCollectionFor(definition.Name, scopeKey);
        await EnsureOwnerIndex(collection, cancellationToken);
        var claim = Builders<UniqueConstraintValueIndex>.Update
            .SetOnInsert(_ => _.Value, value)
            .SetOnInsert(_ => _.EventSourceId, eventSourceId)
            .SetOnInsert(_ => _.SequenceNumber, sequenceNumber);
        var recordSubject = subject is null
            ? Builders<UniqueConstraintValueIndex>.Update.Unset(_ => _.Subject)
            : Builders<UniqueConstraintValueIndex>.Update.Set(_ => _.Subject, subject.Value);
        var update = Builders<UniqueConstraintValueIndex>.Update.Combine(claim, recordSubject);

        try
        {
            await collection.UpdateOneAsync(
                _ => _.Value == value && _.EventSourceId == eventSourceId,
                update,
                new UpdateOptions { IsUpsert = true },
                cancellationToken);
        }
        catch (MongoWriteException error) when (error.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            using var found = await collection.FindAsync(_ => _.Value == value, cancellationToken: cancellationToken);
            var winner = await found.FirstOrDefaultAsync(cancellationToken);
            if (winner?.EventSourceId != eventSourceId)
            {
                throw new DuplicateUniqueConstraintValue(definition.Name, eventSourceId);
            }
        }
    }

    /// <summary>
    /// Releases every claim held by the source in the selected scope.
    /// </summary>
    /// <param name="eventSourceId">The owner.</param>
    /// <param name="definition">The constraint.</param>
    /// <param name="scopeKey">The resolved scope.</param>
    /// <param name="cancellationToken">Cancellation of the write.</param>
    /// <returns>Awaitable task.</returns>
    public async Task Remove(EventSourceId eventSourceId, UniqueConstraintDefinition definition, string scopeKey = "", CancellationToken cancellationToken = default)
    {
        if (definition.Mode == UniqueConstraintMode.PerValue)
        {
            await GetValuesCollectionFor(definition.Name, scopeKey).DeleteManyAsync(_ => _.EventSourceId == eventSourceId, cancellationToken);
        }
        else
        {
            await GetCollectionFor(definition.Name, scopeKey).DeleteOneAsync(_ => _.EventSourceId == eventSourceId, cancellationToken);
        }
    }

    /// <summary>
    /// Releases one value only if the source owns it.
    /// </summary>
    /// <param name="eventSourceId">The owner.</param>
    /// <param name="definition">The constraint.</param>
    /// <param name="value">The value to release.</param>
    /// <param name="scopeKey">The resolved scope.</param>
    /// <param name="cancellationToken">Cancellation of the write.</param>
    /// <returns>Awaitable task.</returns>
    public async Task RemoveValue(EventSourceId eventSourceId, UniqueConstraintDefinition definition, UniqueConstraintValue value, string scopeKey = "", CancellationToken cancellationToken = default) =>
        await GetValuesCollectionFor(definition.Name, scopeKey).DeleteOneAsync(_ => _.Value == value && _.EventSourceId == eventSourceId, cancellationToken);

    /// <inheritdoc/>
    public async Task<(long PerEventSource, long PerValue)> RemoveUnhashedValues(CancellationToken cancellationToken = default)
    {
        var database = eventStoreNamespaceDatabase.GetCollection<UniqueConstraintIndex>($"{eventSequenceId}+constraint").Database;
        var collections = new BsonDocument("name", new BsonRegularExpression($"^{Regex.Escape(eventSequenceId.Value)}\\+.+\\+constraint$"));
        using var names = await database.ListCollectionNamesAsync(new ListCollectionNamesOptions { Filter = collections }, cancellationToken);
        var hash = new BsonRegularExpression("\\A[0-9a-f]{64}\\z");
        long sources = 0;
        long values = 0;
        foreach (var name in await names.ToListAsync(cancellationToken))
        {
            if (name.EndsWith("+values+constraint", StringComparison.Ordinal))
            {
                var filter = !Builders<UniqueConstraintValueIndex>.Filter.Regex(row => row.Value, hash);
                var result = await eventStoreNamespaceDatabase.GetCollection<UniqueConstraintValueIndex>(name).DeleteManyAsync(filter, cancellationToken);
                values += result.DeletedCount;
            }
            else
            {
                var filter = !Builders<UniqueConstraintIndex>.Filter.Regex(row => row.Value, hash);
                var result = await eventStoreNamespaceDatabase.GetCollection<UniqueConstraintIndex>(name).DeleteManyAsync(filter, cancellationToken);
                sources += result.DeletedCount;
            }
        }
        return (sources, values);
    }

    /// <inheritdoc/>
    public async Task RemoveForSubject(Subject subject, CancellationToken cancellationToken = default)
    {
        // Both claim shapes store the subject under the same element, so one typed filter serves every collection.
        var database = eventStoreNamespaceDatabase.GetCollection<UniqueConstraintIndex>($"{eventSequenceId}+constraint").Database;
        var filter = new BsonDocument("name", new BsonRegularExpression($"^{Regex.Escape(eventSequenceId.Value)}\\+.+\\+constraint$"));
        using var names = await database.ListCollectionNamesAsync(new ListCollectionNamesOptions { Filter = filter }, cancellationToken);
        foreach (var name in await names.ToListAsync(cancellationToken))
        {
            await eventStoreNamespaceDatabase.GetCollection<UniqueConstraintIndex>(name).DeleteManyAsync(_ => _.Subject == subject.Value, cancellationToken);
        }
    }

    static Task<string> CreateSubjectIndex<TRow>(IMongoCollection<TRow> collection, System.Linq.Expressions.Expression<Func<TRow, object?>> subject, CancellationToken cancellationToken) =>
        collection.Indexes.CreateOneAsync(
            new CreateIndexModel<TRow>(
                Builders<TRow>.IndexKeys.Ascending(subject),
                new CreateIndexOptions { Name = SubjectIndexName, Sparse = true }),
            cancellationToken: cancellationToken);

    static async IAsyncEnumerable<UniqueConstraintValue> Drain<TRow>(IAsyncCursor<TRow> cursor, Func<TRow, UniqueConstraintValue> value, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using (cursor)
        {
            while (await cursor.MoveNextAsync(cancellationToken))
            {
                foreach (var row in cursor.Current) yield return value(row);
            }
        }
    }

    static Task<string> CreateValueIndex(IMongoCollection<UniqueConstraintIndex> collection, bool unique, CancellationToken cancellationToken) =>
        collection.Indexes.CreateOneAsync(
            new CreateIndexModel<UniqueConstraintIndex>(
                Builders<UniqueConstraintIndex>.IndexKeys.Ascending(_ => _.Value),
                new CreateIndexOptions { Name = ValueIndexName, Unique = unique, Background = true }),
            cancellationToken: cancellationToken);

    async IAsyncEnumerable<UniqueConstraintValue> StreamPerValue(EventSourceId eventSourceId, UniqueConstraintDefinition definition, string scopeKey, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var cursor = await GetValuesCollectionFor(definition.Name, scopeKey).FindAsync(row => row.EventSourceId == eventSourceId, new FindOptions<UniqueConstraintValueIndex> { BatchSize = 100 }, cancellationToken);
        await foreach (var value in Drain(cursor, row => row.Value, cancellationToken)) yield return value;
    }

    async IAsyncEnumerable<UniqueConstraintValue> StreamPerEventSource(EventSourceId eventSourceId, UniqueConstraintDefinition definition, string scopeKey, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var cursor = await GetCollectionFor(definition.Name, scopeKey).FindAsync(row => row.EventSourceId == eventSourceId, new FindOptions<UniqueConstraintIndex> { BatchSize = 100 }, cancellationToken);
        await foreach (var value in Drain(cursor, row => row.Value, cancellationToken)) yield return value;
    }

    async Task SavePerEventSource(EventSourceId eventSourceId, ConstraintName name, EventSequenceNumber sequenceNumber, UniqueConstraintValue value, string scopeKey, Subject? subject, CancellationToken cancellationToken)
    {
        var collection = GetCollectionFor(name, scopeKey);
        await EnsureIndex(collection, cancellationToken).ConfigureAwait(false);
        try
        {
            await collection.ReplaceOneAsync(
                u => u.EventSourceId == eventSourceId,
                new UniqueConstraintIndex(eventSourceId, value, sequenceNumber, subject?.Value),
                new ReplaceOptions { IsUpsert = true },
                cancellationToken);
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            throw new DuplicateUniqueConstraintValue(name, eventSourceId);
        }
    }

    IMongoCollection<UniqueConstraintValueIndex> GetValuesCollectionFor(ConstraintName name, string scopeKey) =>
        eventStoreNamespaceDatabase.GetCollection<UniqueConstraintValueIndex>(string.IsNullOrEmpty(scopeKey)
            ? $"{eventSequenceId}+{name}+values+constraint"
            : $"{eventSequenceId}+{name}+{scopeKey}+values+constraint");

    async Task EnsureOwnerIndex(IMongoCollection<UniqueConstraintValueIndex> collection, CancellationToken cancellationToken = default)
    {
        if (_ensuredIndexes.ContainsKey(collection.CollectionNamespace.FullName))
        {
            return;
        }

        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<UniqueConstraintValueIndex>(
                Builders<UniqueConstraintValueIndex>.IndexKeys.Ascending(_ => _.EventSourceId),
                new CreateIndexOptions { Name = "owner" }),
            cancellationToken: cancellationToken);
        await CreateSubjectIndex(collection, _ => _.Subject, cancellationToken);
        _ensuredIndexes.TryAdd(collection.CollectionNamespace.FullName, 0);
    }

    IMongoCollection<UniqueConstraintIndex> GetCollectionFor(ConstraintName constraintName, string scopeKey = "")
    {
        var collectionName = string.IsNullOrEmpty(scopeKey)
            ? $"{eventSequenceId}+{constraintName}+constraint"
            : $"{eventSequenceId}+{constraintName}+{scopeKey}+constraint";
        return eventStoreNamespaceDatabase.GetCollection<UniqueConstraintIndex>(collectionName);
    }

    async Task EnsureIndex(IMongoCollection<UniqueConstraintIndex> collection, CancellationToken cancellationToken = default)
    {
        if (_ensuredIndexes.ContainsKey(collection.CollectionNamespace.FullName))
        {
            return;
        }

        var existing = new HashSet<string>(StringComparer.Ordinal);
        using var indexes = await collection.Indexes.ListAsync(cancellationToken).ConfigureAwait(false);
        await indexes.ForEachAsync(
            index =>
            {
                if (index.TryGetValue("name", out var name)) existing.Add(name.AsString);
            },
            cancellationToken).ConfigureAwait(false);
        if (!existing.Contains(ValueIndexName))
        {
            try
            {
                await CreateValueIndex(collection, unique: true, cancellationToken).ConfigureAwait(false);
            }
            catch (MongoCommandException ex) when (ex.Code == 11000 || ex.Message.Contains("E11000", StringComparison.Ordinal))
            {
                // The collection already contains duplicate values from before the unique index existed, so the
                // unique index cannot be built. Fall back to a non-unique index so lookups are still fast; the
                // stored duplicates need to be reconciled before uniqueness can be enforced again.
                logger.FallingBackToNonUniqueIndex(collection.CollectionNamespace.FullName);
                await CreateValueIndex(collection, unique: false, cancellationToken).ConfigureAwait(false);
            }
        }

        if (!existing.Contains(SubjectIndexName))
        {
            await CreateSubjectIndex(collection, _ => _.Subject, cancellationToken).ConfigureAwait(false);
        }

        _ensuredIndexes.TryAdd(collection.CollectionNamespace.FullName, 0);
    }
}
