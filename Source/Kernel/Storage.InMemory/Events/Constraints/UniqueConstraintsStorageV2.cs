// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints.V2;
using Cratis.DependencyInjection;
using Cratis.Monads;

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints;

/// <summary>
/// Represents the unwired v2 reference store. Construct one instance per store, namespace and sequence.
/// </summary>
/// <remarks>
/// The single gate linearizes authority transitions with every claim mutation and snapshot read.
/// The provider does not discover this type by convention and never shares claims with v1.
/// Mutations short-circuit on an authority failure before invoking the claim helper, under the same gate.
/// </remarks>
[IgnoreConvention]
public partial class UniqueConstraintsStorageV2 : IUniqueConstraintsStorageV2
{
    readonly Lock _gate = new();
    readonly Dictionary<ConstraintName, UniqueConstraintWriteAuthority> _authorities = [];
    readonly Dictionary<(ConstraintName Name, UniqueConstraintMode Mode), UniqueConstraintV2Claims> _claims = [];

    /// <inheritdoc/>
    public Task<(bool IsAllowed, EventSequenceNumber SequenceNumber)> IsAllowed(EventSourceId eventSourceId, UniqueConstraintDefinition definition, UniqueConstraintValue value, UniqueConstraintStorageScope scope, CancellationToken cancellationToken = default)
    {
        ValidateValue(value);
        lock (_gate)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_claims.TryGetValue((definition.Name, definition.Mode), out var claims)
                ? claims.IsAllowed(eventSourceId, value, scope)
                : (true, EventSequenceNumber.Unavailable));
        }
    }

    /// <inheritdoc/>
    public Task<IEnumerable<UniqueConstraintValue>> GetFor(EventSourceId eventSourceId, UniqueConstraintDefinition definition, UniqueConstraintStorageScope scope, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IEnumerable<UniqueConstraintValue>>(Snapshot(eventSourceId, definition, scope));
        }
    }

    /// <inheritdoc/>
    public Task<IAsyncEnumerable<UniqueConstraintValue>> GetForStream(EventSourceId eventSourceId, UniqueConstraintDefinition definition, UniqueConstraintStorageScope scope, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IAsyncEnumerable<UniqueConstraintValue>>(new UniqueConstraintValues(Snapshot(eventSourceId, definition, scope), cancellationToken));
        }
    }

    /// <inheritdoc/>
    public Task<Result<bool, UniqueConstraintWriteFailure>> Save(EventSourceId eventSourceId, UniqueConstraintDefinition definition, EventSequenceNumber sequenceNumber, UniqueConstraintValue value, UniqueConstraintStorageScope scope, UniqueConstraintWriteAuthority expected, Subject? subject = default, CancellationToken cancellationToken = default)
    {
        ValidateValue(value);
        lock (_gate)
        {
            return Outcome(CheckAuthority(definition.Name, expected, cancellationToken)
                ?? ClaimsFor(definition).Save(definition.Mode, value, scope, new(eventSourceId, sequenceNumber, subject)));
        }
    }

    /// <inheritdoc/>
    public Task<Result<bool, UniqueConstraintWriteFailure>> Remove(EventSourceId eventSourceId, UniqueConstraintDefinition definition, UniqueConstraintStorageScope scope, UniqueConstraintWriteAuthority expected, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            return Outcome(CheckAuthority(definition.Name, expected, cancellationToken)
                ?? ClaimsFor(definition).Remove(eventSourceId, scope));
        }
    }

    /// <inheritdoc/>
    public Task<Result<bool, UniqueConstraintWriteFailure>> RemoveValue(EventSourceId eventSourceId, UniqueConstraintDefinition definition, UniqueConstraintValue value, UniqueConstraintStorageScope scope, UniqueConstraintWriteAuthority expected, CancellationToken cancellationToken = default)
    {
        ValidateValue(value);
        lock (_gate)
        {
            return Outcome(CheckAuthority(definition.Name, expected, cancellationToken)
                ?? ClaimsFor(definition).RemoveValue(eventSourceId, definition.Mode, value, scope));
        }
    }

    /// <inheritdoc/>
    public Task<Result<bool, UniqueConstraintWriteFailure>> ClearValues(UniqueConstraintDefinition definition, UniqueConstraintStorageScope scope, UniqueConstraintWriteAuthority expected, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            return Outcome(CheckAuthority(definition.Name, expected, cancellationToken)
                ?? ClaimsFor(definition).Clear(scope));
        }
    }

    /// <inheritdoc/>
    public Task<Result<bool, UniqueConstraintWriteFailure>> RemoveAllFor(UniqueConstraintDefinition definition, UniqueConstraintWriteAuthority expected, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            return Outcome(CheckAuthority(definition.Name, expected, cancellationToken)
                ?? ClaimsFor(definition).Clear());
        }
    }

    /// <inheritdoc/>
    public Task<Result<bool, UniqueConstraintWriteFailure>> RemoveForSubject(UniqueConstraintDefinition definition, Subject subject, UniqueConstraintWriteAuthority expected, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            return Outcome(CheckAuthority(definition.Name, expected, cancellationToken)
                ?? ClaimsFor(definition).RemoveForSubject(subject));
        }
    }

    static Task<Result<bool, UniqueConstraintWriteFailure>> Outcome(UniqueConstraintWriteFailure? failure) => Task.FromResult(failure is null
        ? Result<bool, UniqueConstraintWriteFailure>.Success(true)
        : Result<bool, UniqueConstraintWriteFailure>.Failed(failure.Value));

    static void ValidateValue(UniqueConstraintValue value)
    {
        if (value.Value.Length != 64 || !value.Value.All(_ => _ is >= '0' and <= '9' or >= 'a' and <= 'f'))
        {
            throw new InvalidUniqueConstraintV2Input(nameof(UniqueConstraintValue));
        }
    }

    UniqueConstraintV2Claims ClaimsFor(UniqueConstraintDefinition definition)
    {
        var key = (definition.Name, definition.Mode);
        if (!_claims.TryGetValue(key, out var claims))
        {
            claims = new();
            _claims.Add(key, claims);
        }

        return claims;
    }

    UniqueConstraintValue[] Snapshot(EventSourceId owner, UniqueConstraintDefinition definition, UniqueConstraintStorageScope scope) =>
        _claims.TryGetValue((definition.Name, definition.Mode), out var claims) ? claims.GetFor(owner, scope) : [];
}
