// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints.V2;
using Cratis.Monads;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.V2;

public sealed partial class UniqueConstraintsStorageV2
{
    /// <inheritdoc/>
    public Task<Result<bool, UniqueConstraintWriteFailure>> Save(EventSourceId eventSourceId, UniqueConstraintDefinition definition, EventSequenceNumber sequenceNumber, UniqueConstraintValue value, UniqueConstraintStorageScope scope, UniqueConstraintWriteAuthority expected, Subject? subject = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateOwner(eventSourceId);
        ValidateScope(scope);
        ValidateValue(value);
        var claim = new V2Claim(scope.Value, eventSourceId.Value, value.Value, sequenceNumber.Value, subject?.Value);
        return Mutate(definition, expected, context => SaveClaim(context, definition.Mode, claim, cancellationToken), cancellationToken);
    }

    /// <inheritdoc/>
    public Task<Result<bool, UniqueConstraintWriteFailure>> Remove(EventSourceId eventSourceId, UniqueConstraintDefinition definition, UniqueConstraintStorageScope scope, UniqueConstraintWriteAuthority expected, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateOwner(eventSourceId);
        ValidateScope(scope);
        return Mutate(definition, expected, context => Delete(context.Claims.Where(row => row.Scope == scope.Value && row.EventSourceId == eventSourceId.Value), cancellationToken), cancellationToken);
    }

    /// <inheritdoc/>
    public Task<Result<bool, UniqueConstraintWriteFailure>> RemoveValue(EventSourceId eventSourceId, UniqueConstraintDefinition definition, UniqueConstraintValue value, UniqueConstraintStorageScope scope, UniqueConstraintWriteAuthority expected, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateOwner(eventSourceId);
        ValidateScope(scope);
        ValidateValue(value);
        return Mutate(definition, expected, context => definition.Mode == UniqueConstraintMode.PerValue
            ? Delete(context.Claims.Where(row => row.Scope == scope.Value && row.Value == value.Value && row.EventSourceId == eventSourceId.Value), cancellationToken)
            : Task.FromResult<UniqueConstraintWriteFailure?>(null), cancellationToken);
    }

    /// <inheritdoc/>
    public Task<Result<bool, UniqueConstraintWriteFailure>> ClearValues(UniqueConstraintDefinition definition, UniqueConstraintStorageScope scope, UniqueConstraintWriteAuthority expected, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateScope(scope);
        return Mutate(definition, expected, context => Delete(context.Claims.Where(row => row.Scope == scope.Value), cancellationToken), cancellationToken);
    }

    /// <inheritdoc/>
    public Task<Result<bool, UniqueConstraintWriteFailure>> RemoveAllFor(UniqueConstraintDefinition definition, UniqueConstraintWriteAuthority expected, CancellationToken cancellationToken = default) =>
        Mutate(definition, expected, context => Delete(context.Claims, cancellationToken), cancellationToken);

    /// <inheritdoc/>
    public Task<Result<bool, UniqueConstraintWriteFailure>> RemoveForSubject(UniqueConstraintDefinition definition, Subject subject, UniqueConstraintWriteAuthority expected, CancellationToken cancellationToken = default) =>
        Mutate(definition, expected, context => DeleteSubject(context, subject, cancellationToken), cancellationToken);

    async Task<Result<bool, UniqueConstraintWriteFailure>> Mutate(UniqueConstraintDefinition definition, UniqueConstraintWriteAuthority expected, Func<V2DbContext, Task<UniqueConstraintWriteFailure?>> write, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateAuthority(expected);
        var failure = await CheckAuthority(definition.Name, expected, cancellationToken);
        if (failure is not null) return Outcome(failure);
        var table = await _schema.Resolve(Binding(definition), create: true, cancellationToken);
        await using var context = _schema.Context(table, definition.Mode);
        await using var transaction = await V2Transactions.Begin(context, cancellationToken);
        failure = await Fence(context, definition.Name, expected, cancellationToken);
        if (failure is not null) return Outcome(failure);
        failure = await write(context);
        if (failure is not null) return Outcome(failure);
        await transaction.CommitAsync(cancellationToken);
        return Outcome(null);
    }

    static async Task<UniqueConstraintWriteFailure?> Delete(IQueryable<V2Claim> claims, CancellationToken cancellationToken)
    {
        await claims.ExecuteDeleteAsync(cancellationToken);
        return null;
    }

    static async Task<UniqueConstraintWriteFailure?> DeleteSubject(V2DbContext context, Subject subject, CancellationToken cancellationToken)
    {
        // Unbounded subject is deliberately not indexed or compared using provider text padding/collation.
        var claims = await context.Claims.AsNoTracking().ToArrayAsync(cancellationToken);
        foreach (var claim in claims.Where(claim => string.Equals(claim.Subject, subject.Value, StringComparison.Ordinal)))
        {
            await context.Claims.Where(row => row.Scope == claim.Scope && row.Value == claim.Value).ExecuteDeleteAsync(cancellationToken);
        }

        return null;
    }
}
