// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.Events.Constraints.V2;
using Cratis.DependencyInjection;
using Cratis.Monads;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.V2;

/// <summary>
/// Dormant authority-fenced SQL v2 storage, constructed explicitly for one namespace database and sequence.
/// </summary>
/// <remarks>
/// Owners are at most 450 UTF-16 code units. SQL Server uses binary collation but its ordinary indexed
/// equality still pads trailing spaces: owner and owner-space identify the same owner there. No digest
/// or truncation changes owner identity. Subjects are unbounded, nullable and matched exactly in memory.
/// A factory must return options for a fresh operation-owned connection, not a shared open connection.
/// </remarks>
/// <param name="eventSequenceId">The logical sequence, retained in every binding.</param>
/// <param name="createOptions">The explicit namespace database configuration factory.</param>
[IgnoreConvention]
public sealed partial class UniqueConstraintsStorageV2(EventSequenceId eventSequenceId, Func<DbContextOptions> createOptions) : IUniqueConstraintsStorageV2
{
    readonly V2Schema _schema = new(createOptions);

    V2Binding Binding(UniqueConstraintDefinition definition)
    {
        var id = V2Identity.Binding(eventSequenceId.Value, definition.Name.Value, definition.Mode);
        return new(id, eventSequenceId.Value, definition.Name.Value, definition.Mode, V2Identity.Table(id, definition.Mode));
    }

    static void ValidateAuthority(UniqueConstraintWriteAuthority authority)
    {
        if (authority is null || authority.Phase is null) throw new InvalidUniqueConstraintV2Input(nameof(UniqueConstraintWriteAuthority));
    }

    static void ValidateScope(UniqueConstraintStorageScope scope)
    {
        if (scope is null) throw new InvalidUniqueConstraintV2Input(nameof(UniqueConstraintStorageScope));
    }

    static void ValidateValue(UniqueConstraintValue value)
    {
        if (value is null || value.Value.Length != 64 || !value.Value.All(character => character is >= '0' and <= '9' or >= 'a' and <= 'f'))
        {
            throw new InvalidUniqueConstraintV2Input(nameof(UniqueConstraintValue));
        }
    }

    static void ValidateOwner(EventSourceId owner)
    {
        if (owner.Value.Length > V2Identity.OwnerLimit) throw new UniqueConstraintV2OwnerTooLong();
    }

    static Result<bool, UniqueConstraintWriteFailure> Outcome(UniqueConstraintWriteFailure? failure) => failure is null
        ? Result<bool, UniqueConstraintWriteFailure>.Success(true)
        : Result<bool, UniqueConstraintWriteFailure>.Failed(failure.Value);
}
