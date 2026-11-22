// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints.V2;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints.V2;

public sealed partial class UniqueConstraintsStorageV2
{
    /// <inheritdoc/>
    public async Task<(bool IsAllowed, EventSequenceNumber SequenceNumber)> IsAllowed(EventSourceId eventSourceId, UniqueConstraintDefinition definition, UniqueConstraintValue value, UniqueConstraintStorageScope scope, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (eventSourceId is null) throw new InvalidUniqueConstraintV2Input(nameof(eventSourceId));
        ValidateText(eventSourceId.Value);
        ValidateValue(value);
        ValidateScope(scope);
        var physical = await _schema.Resolve(Binding(definition), false, cancellationToken);
        if (physical is null) return (true, EventSequenceNumber.Unavailable);
        var filter = new BsonDocument { { "Scope", scope.Value }, { "Value", value.Value } };
        var existing = await _database.GetCollection<BsonDocument>(physical).Find(filter, new FindOptions { Collation = V2Schema.Simple }).SingleOrDefaultAsync(cancellationToken);
        return existing is null ? (true, EventSequenceNumber.Unavailable) : (existing["EventSourceId"] == eventSourceId.Value, V2Documents.Sequence(existing["SequenceNumber"]));
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<UniqueConstraintValue>> GetFor(EventSourceId eventSourceId, UniqueConstraintDefinition definition, UniqueConstraintStorageScope scope, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (eventSourceId is null) throw new InvalidUniqueConstraintV2Input(nameof(eventSourceId));
        ValidateText(eventSourceId.Value);
        ValidateScope(scope);
        var physical = await _schema.Resolve(Binding(definition), false, cancellationToken);
        if (physical is null) return [];
        var filter = new BsonDocument { { "Scope", scope.Value }, { "EventSourceId", eventSourceId.Value } };
        var rows = await _database.GetCollection<BsonDocument>(physical).Find(filter, new FindOptions { Collation = V2Schema.Simple }).ToListAsync(cancellationToken);
        return rows.Select(row => new UniqueConstraintValue(row["Value"].AsString)).ToArray();
    }

    /// <inheritdoc/>
    public async Task<IAsyncEnumerable<UniqueConstraintValue>> GetForStream(EventSourceId eventSourceId, UniqueConstraintDefinition definition, UniqueConstraintStorageScope scope, CancellationToken cancellationToken = default) =>
        Stream((await GetFor(eventSourceId, definition, scope, cancellationToken)).ToArray(), cancellationToken);

    static async IAsyncEnumerable<UniqueConstraintValue> Stream(UniqueConstraintValue[] values, CancellationToken methodToken, [EnumeratorCancellation] CancellationToken enumeratorToken = default)
    {
        await Task.CompletedTask;
        methodToken.ThrowIfCancellationRequested();
        enumeratorToken.ThrowIfCancellationRequested();
        foreach (var value in values)
        {
            methodToken.ThrowIfCancellationRequested();
            enumeratorToken.ThrowIfCancellationRequested();
            yield return value;
        }
    }
}
