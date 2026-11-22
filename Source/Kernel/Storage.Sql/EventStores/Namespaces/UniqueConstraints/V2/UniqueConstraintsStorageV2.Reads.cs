// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints.V2;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.V2;

public sealed partial class UniqueConstraintsStorageV2
{
    /// <inheritdoc/>
    public async Task<(bool IsAllowed, EventSequenceNumber SequenceNumber)> IsAllowed(EventSourceId eventSourceId, UniqueConstraintDefinition definition, UniqueConstraintValue value, UniqueConstraintStorageScope scope, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateOwner(eventSourceId);
        ValidateScope(scope);
        ValidateValue(value);
        var table = await _schema.Resolve(Binding(definition), create: false, cancellationToken);
        if (table is null) return (true, EventSequenceNumber.Unavailable);
        await using var context = _schema.Context(table, definition.Mode);
        var claim = await context.Claims.AsNoTracking().Where(row => row.Scope == scope.Value && row.Value == value.Value)
            .Select(row => new { Allowed = row.EventSourceId == eventSourceId.Value, row.SequenceNumber }).SingleOrDefaultAsync(cancellationToken);
        return claim is null ? (true, EventSequenceNumber.Unavailable) : (claim.Allowed, new((ulong)claim.SequenceNumber));
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<UniqueConstraintValue>> GetFor(EventSourceId eventSourceId, UniqueConstraintDefinition definition, UniqueConstraintStorageScope scope, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateOwner(eventSourceId);
        ValidateScope(scope);
        var table = await _schema.Resolve(Binding(definition), create: false, cancellationToken);
        if (table is null) return [];
        await using var context = _schema.Context(table, definition.Mode);
        var values = await context.Claims.AsNoTracking().Where(row => row.Scope == scope.Value && row.EventSourceId == eventSourceId.Value).Select(row => row.Value).ToArrayAsync(cancellationToken);
        return values.Select(value => new UniqueConstraintValue(value)).ToArray();
    }

    /// <inheritdoc/>
    public async Task<IAsyncEnumerable<UniqueConstraintValue>> GetForStream(EventSourceId eventSourceId, UniqueConstraintDefinition definition, UniqueConstraintStorageScope scope, CancellationToken cancellationToken = default)
    {
        var snapshot = await GetFor(eventSourceId, definition, scope, cancellationToken);
        return Values(snapshot, cancellationToken);
    }

    static async IAsyncEnumerable<UniqueConstraintValue> Values(IEnumerable<UniqueConstraintValue> snapshot, CancellationToken readToken, [EnumeratorCancellation] CancellationToken enumerationToken = default)
    {
        await Task.CompletedTask;
        readToken.ThrowIfCancellationRequested();
        enumerationToken.ThrowIfCancellationRequested();
        foreach (var value in snapshot)
        {
            readToken.ThrowIfCancellationRequested();
            enumerationToken.ThrowIfCancellationRequested();
            yield return value;
        }
    }
}
