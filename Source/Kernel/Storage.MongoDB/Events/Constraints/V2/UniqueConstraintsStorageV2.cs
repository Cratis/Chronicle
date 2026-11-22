// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.Events.Constraints.V2;
using Cratis.DependencyInjection;
using Cratis.Monads;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints.V2;

/// <summary>
/// Represents additive, unwired v2 storage in one explicitly selected MongoDB namespace and sequence.
/// No existing resolver, v1 collection or runtime admission path uses this provider.
/// </summary>
[IgnoreConvention]
public sealed partial class UniqueConstraintsStorageV2 : IUniqueConstraintsStorageV2
{
    readonly IMongoDatabase _database;
    readonly string _sequence;
    readonly V2Schema _schema;
    readonly V2Transactions _transactions;

    /// <summary>Initializes a new instance of the <see cref="UniqueConstraintsStorageV2"/> class.</summary>
    /// <param name="database">The explicitly selected namespace database.</param>
    /// <param name="eventSequenceId">The event sequence to isolate.</param>
    /// <exception cref="InvalidUniqueConstraintV2Input">The database or logical identity is invalid.</exception>
    public UniqueConstraintsStorageV2(IMongoDatabase database, EventSequenceId eventSequenceId)
    {
        if (database is null || eventSequenceId is null) throw new InvalidUniqueConstraintV2Input("storage");
        V2Identity.Encode(eventSequenceId.Value);
        _database = database.WithReadPreference(ReadPreference.Primary).WithReadConcern(ReadConcern.Majority).WithWriteConcern(WriteConcern.WMajority);
        _sequence = eventSequenceId.Value;
        _schema = new(_database);
        _transactions = new(_database, ReadConcern.Snapshot);
    }

    static void ValidateText(string? text)
    {
        if (text is null) throw new InvalidUniqueConstraintV2Input("identity");
        V2Identity.Encode(text);
    }

    static void ValidateScope(UniqueConstraintStorageScope scope)
    {
        if (scope is null) throw new InvalidUniqueConstraintV2Input(nameof(scope));
        _ = new UniqueConstraintStorageScope(scope.Value);
    }

    static void ValidateValue(UniqueConstraintValue value)
    {
        if (value is null) throw new InvalidUniqueConstraintV2Input(nameof(value));
        if (value.Value.Length != 64) throw new InvalidUniqueConstraintV2Input(nameof(value));
        if (value.Value.Any(character => character is not (>= '0' and <= '9' or >= 'a' and <= 'f'))) throw new InvalidUniqueConstraintV2Input(nameof(value));
    }

    static void ValidatePhase(UniqueConstraintWritePhase phase)
    {
        if (phase is null || phase.RunId?.Value == Guid.Empty) throw new InvalidUniqueConstraintV2Input(nameof(phase));
    }

    static void ValidateAuthority(UniqueConstraintWriteAuthority authority)
    {
        if (authority is null) throw new InvalidUniqueConstraintV2Input(nameof(authority));
        if (authority.Epoch is null) throw new InvalidUniqueConstraintV2Input(nameof(authority));
        if (authority.Epoch.Value == Guid.Empty) throw new InvalidUniqueConstraintV2Input(nameof(authority));
        if (authority.Revision is null) throw new InvalidUniqueConstraintV2Input(nameof(authority));
        if (authority.Revision.Value < 0) throw new InvalidUniqueConstraintV2Input(nameof(authority));
        ValidatePhase(authority.Phase);
    }

    static Result<bool, UniqueConstraintWriteFailure> Outcome(UniqueConstraintWriteFailure? failure) => failure is null
        ? Result<bool, UniqueConstraintWriteFailure>.Success(true)
        : Result<bool, UniqueConstraintWriteFailure>.Failed(failure.Value);

    V2Binding Binding(UniqueConstraintDefinition definition)
    {
        if (definition is null || !Enum.IsDefined(definition.Mode)) throw new InvalidUniqueConstraintV2Input(nameof(definition));
        if (definition.Name is null) throw new InvalidUniqueConstraintV2Input(nameof(definition));
        ValidateText(definition.Name.Value);
        return new(_sequence, definition.Name.Value, definition.Mode);
    }
}
