// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints;

public partial class UniqueConstraintsStorage
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<(bool IsAllowed, EventSequenceNumber SequenceNumber)>> IsAllowedMany(IReadOnlyList<UniqueConstraintClaim> claims, UniqueConstraintDefinition definition, string scopeKey = "", CancellationToken cancellationToken = default)
    {
        var values = claims.Select(claim => claim.Value).Distinct().ToArray();
        Dictionary<UniqueConstraintValue, (EventSourceId EventSourceId, EventSequenceNumber SequenceNumber)> owners;
        if (definition.Mode == UniqueConstraintMode.PerValue)
        {
            var collection = GetValuesCollectionFor(definition.Name, scopeKey);
            await EnsureOwnerIndex(collection, cancellationToken);
            owners = await FirstOwnersOf(collection, Builders<UniqueConstraintValueIndex>.Filter.In(_ => _.Value, values), row => row.Value, row => (row.EventSourceId, row.SequenceNumber), values.Length, cancellationToken);
        }
        else
        {
            var collection = GetCollectionFor(definition.Name, scopeKey);
            await EnsureIndex(collection, cancellationToken).ConfigureAwait(false);
            owners = await FirstOwnersOf(collection, Builders<UniqueConstraintIndex>.Filter.In(_ => _.Value, values), row => row.Value, row => (row.EventSourceId, row.SequenceNumber), values.Length, cancellationToken);
        }

        // The same answer IsAllowed gives: a value nobody holds is allowed, a held value only for its holder.
        return [.. claims.Select(claim => owners.TryGetValue(claim.Value, out var owner)
            ? (owner.EventSourceId == claim.EventSourceId, owner.SequenceNumber)
            : (true, EventSequenceNumber.Unavailable))];
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<int>> ApplyChanges(UniqueConstraintDefinition definition, IReadOnlyList<UniqueConstraintIndexChange> changes, CancellationToken cancellationToken = default)
    {
        // Releasing a single value always addresses the retained-values collection, so a per-event-source constraint
        // receiving one would span two collections. The updater never produces that; should it, keep the exact
        // single-item behavior rather than reproduce it here.
        if (definition.Mode != UniqueConstraintMode.PerValue && changes.Any(change => change.Kind == UniqueConstraintIndexChangeKind.RemoveValue))
        {
            return await UniqueConstraintIndexChanges.ApplySequentially(this, definition, changes, cancellationToken);
        }

        var duplicates = new List<int>();

        // Each scope is its own collection, so scopes are independent of each other. Order matters only within one.
        foreach (var scope in changes.Select((change, index) => (Change: change, Index: index)).GroupBy(_ => _.Change.ScopeKey))
        {
            var inScope = scope.ToArray();
            var refused = definition.Mode == UniqueConstraintMode.PerValue
                ? await ApplyPerValueChanges(definition, scope.Key, [.. inScope.Select(_ => _.Change)], cancellationToken)
                : await ApplyPerEventSourceChanges(definition, scope.Key, [.. inScope.Select(_ => _.Change)], cancellationToken);
            duplicates.AddRange(refused.Select(index => inScope[index].Index));
        }

        return duplicates;
    }

    static async Task<Dictionary<UniqueConstraintValue, (EventSourceId EventSourceId, EventSequenceNumber SequenceNumber)>> FirstOwnersOf<TRow>(
        IMongoCollection<TRow> collection,
        FilterDefinition<TRow> filter,
        Func<TRow, UniqueConstraintValue> valueOf,
        Func<TRow, (EventSourceId EventSourceId, EventSequenceNumber SequenceNumber)> ownerOf,
        int valueCount,
        CancellationToken cancellationToken)
    {
        var owners = new Dictionary<UniqueConstraintValue, (EventSourceId, EventSequenceNumber)>();
        if (valueCount == 0) return owners;

        // IsAllowed takes the first row holding a value; reading them in the same order keeps that answer.
        using var cursor = await collection.FindAsync(filter, cancellationToken: cancellationToken);
        while (await cursor.MoveNextAsync(cancellationToken))
        {
            foreach (var row in cursor.Current)
            {
                owners.TryAdd(valueOf(row), ownerOf(row));
            }
        }

        return owners;
    }

    /// <summary>
    /// Writes the models as one ordered bulk write, continuing past every duplicate key the way the single-item writes
    /// do, so one refused claim does not stop the changes after it.
    /// </summary>
    /// <typeparam name="TRow">The type of row.</typeparam>
    /// <param name="collection">The collection to write to.</param>
    /// <param name="models">The models, in order.</param>
    /// <param name="isRefused">Decides whether the duplicate key at a position refuses that claim.</param>
    /// <param name="cancellationToken">Cancellation of the write.</param>
    /// <returns>The positions of the refused claims.</returns>
    static async Task<IEnumerable<int>> WriteInOrder<TRow>(
        IMongoCollection<TRow> collection,
        WriteModel<TRow>[] models,
        Func<int, CancellationToken, Task<bool>> isRefused,
        CancellationToken cancellationToken)
    {
        var refused = new List<int>();
        var start = 0;
        while (start < models.Length)
        {
            try
            {
                await collection.BulkWriteAsync(models[start..], new BulkWriteOptions { IsOrdered = true }, cancellationToken).ConfigureAwait(false);
                break;
            }
            catch (MongoBulkWriteException<TRow> error) when (error.WriteErrors.Count == 1 && error.WriteErrors[0].Category == ServerErrorCategory.DuplicateKey)
            {
                // An ordered bulk write stops at its first error, so everything before it is applied and nothing after it.
                var position = start + error.WriteErrors[0].Index;
                if (await isRefused(position, cancellationToken)) refused.Add(position);
                start = position + 1;
            }
        }

        return refused;
    }

    async Task<IEnumerable<int>> ApplyPerEventSourceChanges(UniqueConstraintDefinition definition, string scopeKey, UniqueConstraintIndexChange[] changes, CancellationToken cancellationToken)
    {
        var collection = GetCollectionFor(definition.Name, scopeKey);
        await EnsureIndex(collection, cancellationToken).ConfigureAwait(false);
        var models = changes.Select(WriteModelFor).ToArray();

        // A duplicate key here is the value index refusing a value another event source holds - Save's duplicate.
        return await WriteInOrder(collection, models, (_, _) => Task.FromResult(true), cancellationToken);

        static WriteModel<UniqueConstraintIndex> WriteModelFor(UniqueConstraintIndexChange change) => change.Kind == UniqueConstraintIndexChangeKind.Save
            ? new ReplaceOneModel<UniqueConstraintIndex>(
                Builders<UniqueConstraintIndex>.Filter.Eq(_ => _.EventSourceId, change.EventSourceId),
                new UniqueConstraintIndex(change.EventSourceId, change.Value!, change.SequenceNumber, change.Subject?.Value))
            { IsUpsert = true }
            : new DeleteOneModel<UniqueConstraintIndex>(Builders<UniqueConstraintIndex>.Filter.Eq(_ => _.EventSourceId, change.EventSourceId));
    }

    async Task<IEnumerable<int>> ApplyPerValueChanges(UniqueConstraintDefinition definition, string scopeKey, UniqueConstraintIndexChange[] changes, CancellationToken cancellationToken)
    {
        var collection = GetValuesCollectionFor(definition.Name, scopeKey);
        await EnsureOwnerIndex(collection, cancellationToken);
        var models = changes.Select(WriteModelFor).ToArray();

        // The same resolution Save makes: a duplicate key only refuses the claim when another event source won the value.
        return await WriteInOrder(
            collection,
            models,
            async (index, token) =>
            {
                using var found = await collection.FindAsync(_ => _.Value == changes[index].Value, cancellationToken: token);
                var winner = await found.FirstOrDefaultAsync(token);
                return winner?.EventSourceId != changes[index].EventSourceId;
            },
            cancellationToken);

        static WriteModel<UniqueConstraintValueIndex> WriteModelFor(UniqueConstraintIndexChange change) => change.Kind switch
        {
            UniqueConstraintIndexChangeKind.Save => new UpdateOneModel<UniqueConstraintValueIndex>(OwnedValue(change), ClaimOf(change)) { IsUpsert = true },
            UniqueConstraintIndexChangeKind.RemoveValue => new DeleteOneModel<UniqueConstraintValueIndex>(OwnedValue(change)),
            _ => new DeleteManyModel<UniqueConstraintValueIndex>(Builders<UniqueConstraintValueIndex>.Filter.Eq(_ => _.EventSourceId, change.EventSourceId))
        };

        static FilterDefinition<UniqueConstraintValueIndex> OwnedValue(UniqueConstraintIndexChange change) =>
            Builders<UniqueConstraintValueIndex>.Filter.Where(_ => _.Value == change.Value && _.EventSourceId == change.EventSourceId);

        static UpdateDefinition<UniqueConstraintValueIndex> ClaimOf(UniqueConstraintIndexChange change)
        {
            var claim = Builders<UniqueConstraintValueIndex>.Update
                .SetOnInsert(_ => _.Value, change.Value)
                .SetOnInsert(_ => _.EventSourceId, change.EventSourceId)
                .SetOnInsert(_ => _.SequenceNumber, change.SequenceNumber);
            var recordSubject = change.Subject is null
                ? Builders<UniqueConstraintValueIndex>.Update.Unset(_ => _.Subject)
                : Builders<UniqueConstraintValueIndex>.Update.Set(_ => _.Subject, change.Subject.Value);
            return Builders<UniqueConstraintValueIndex>.Update.Combine(claim, recordSubject);
        }
    }
}
