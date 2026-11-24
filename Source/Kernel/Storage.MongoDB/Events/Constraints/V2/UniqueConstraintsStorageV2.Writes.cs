// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints.V2;
using Cratis.Monads;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints.V2;

public sealed partial class UniqueConstraintsStorageV2
{
    /// <inheritdoc/>
    public Task<Result<bool, UniqueConstraintWriteFailure>> Save(EventSourceId eventSourceId, UniqueConstraintDefinition definition, EventSequenceNumber sequenceNumber, UniqueConstraintValue value, UniqueConstraintStorageScope scope, UniqueConstraintWriteAuthority expected, Subject? subject = default, CancellationToken cancellationToken = default)
    {
        ValidateOwnerScope(eventSourceId, scope, cancellationToken);
        ValidateValue(value);
        if (sequenceNumber is null) throw new InvalidUniqueConstraintV2Input(nameof(sequenceNumber));
        if (subject is not null) ValidateText(subject.Value);
        var binding = Binding(definition);
        var key = new BsonDocument("Scope", scope.Value);
        if (definition.Mode == UniqueConstraintMode.PerValue) key.Add("Value", value.Value);
        else key.Add("EventSourceId", eventSourceId.Value);
        var document = new BsonDocument
        {
            { "_id", key }, { "Scope", scope.Value }, { "Value", value.Value },
            { "EventSourceId", eventSourceId.Value }, { "SequenceNumber", V2Documents.Sequence(sequenceNumber) }
        };
        if (subject is not null) document.Add("Subject", subject.Value);
        return Mutate((binding, true), expected, attempt => SaveClaim(attempt.Session, attempt.Claims, (document, definition.Mode), cancellationToken), cancellationToken);
    }

    /// <inheritdoc/>
    public Task<Result<bool, UniqueConstraintWriteFailure>> Remove(EventSourceId eventSourceId, UniqueConstraintDefinition definition, UniqueConstraintStorageScope scope, UniqueConstraintWriteAuthority expected, CancellationToken cancellationToken = default)
    {
        ValidateOwnerScope(eventSourceId, scope, cancellationToken);
        var filter = new BsonDocument { { "Scope", scope.Value }, { "EventSourceId", eventSourceId.Value } };
        return RemoveMatching(definition, expected, filter, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<Result<bool, UniqueConstraintWriteFailure>> RemoveValue(EventSourceId eventSourceId, UniqueConstraintDefinition definition, UniqueConstraintValue value, UniqueConstraintStorageScope scope, UniqueConstraintWriteAuthority expected, CancellationToken cancellationToken = default)
    {
        ValidateOwnerScope(eventSourceId, scope, cancellationToken);
        ValidateValue(value);
        var binding = Binding(definition);
        var filter = new BsonDocument { { "Scope", scope.Value }, { "EventSourceId", eventSourceId.Value }, { "Value", value.Value } };
        return Mutate(
            (binding, false),
            expected,
            attempt => definition.Mode == UniqueConstraintMode.PerValue
                ? DeleteClaims(attempt.Session, attempt.Claims, filter, cancellationToken)
                : Task.FromResult<UniqueConstraintWriteFailure?>(null),
            cancellationToken);
    }

    /// <inheritdoc/>
    public Task<Result<bool, UniqueConstraintWriteFailure>> ClearValues(UniqueConstraintDefinition definition, UniqueConstraintStorageScope scope, UniqueConstraintWriteAuthority expected, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateScope(scope);
        return RemoveMatching(definition, expected, new("Scope", scope.Value), cancellationToken);
    }

    /// <inheritdoc/>
    public Task<Result<bool, UniqueConstraintWriteFailure>> RemoveAllFor(UniqueConstraintDefinition definition, UniqueConstraintWriteAuthority expected, CancellationToken cancellationToken = default) =>
        RemoveMatching(definition, expected, new(), cancellationToken);

    /// <inheritdoc/>
    public Task<Result<bool, UniqueConstraintWriteFailure>> RemoveForSubject(UniqueConstraintDefinition definition, Subject subject, UniqueConstraintWriteAuthority expected, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (subject is null) throw new InvalidUniqueConstraintV2Input(nameof(subject));
        ValidateText(subject.Value);
        return RemoveMatching(definition, expected, new("Subject", subject.Value), cancellationToken);
    }

    static void ValidateOwnerScope(EventSourceId owner, UniqueConstraintStorageScope scope, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateText(owner?.Value);
        ValidateScope(scope);
    }

    Task<Result<bool, UniqueConstraintWriteFailure>> RemoveMatching(UniqueConstraintDefinition definition, UniqueConstraintWriteAuthority expected, BsonDocument filter, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Mutate((Binding(definition), false), expected, attempt => DeleteClaims(attempt.Session, attempt.Claims, filter, cancellationToken), cancellationToken);
    }

    async Task<Result<bool, UniqueConstraintWriteFailure>> Mutate((V2Binding Binding, bool Create) mutation, UniqueConstraintWriteAuthority expected, Func<(IClientSessionHandle Session, IMongoCollection<BsonDocument> Claims), Task<UniqueConstraintWriteFailure?>> write, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateAuthority(expected);
        var failure = await CheckAuthority(mutation.Binding.Constraint, expected, cancellationToken);
        if (failure is not null) return Outcome(failure);
        await V2Topology.Require(_database, cancellationToken);
        var physical = await _schema.Resolve(mutation.Binding, mutation.Create, cancellationToken);
        return await _transactions.Run(
            async session =>
            {
                var refusal = await Fence(session, mutation.Binding, expected, cancellationToken);
                if (refusal is not null) return (Outcome(refusal), false);
                if (physical is not null) refusal = await write((session, _database.GetCollection<BsonDocument>(physical)));
                return (Outcome(refusal), refusal is null);
            },
            cancellationToken,
            retryDuplicate: true);
    }
}
