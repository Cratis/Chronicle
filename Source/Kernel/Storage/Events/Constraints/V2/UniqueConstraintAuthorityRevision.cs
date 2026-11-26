// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Chronicle.Storage.Events.Constraints.V2;

/// <summary>
/// Represents a nonnegative authority revision that is never reset or reused.
/// </summary>
/// <param name="Value">The revision.</param>
public sealed record UniqueConstraintAuthorityRevision(long Value) : ConceptAs<long>(Value >= 0 ? Value : throw new InvalidUniqueConstraintV2Input(nameof(UniqueConstraintAuthorityRevision)))
{
    /// <summary>
    /// Advances once, refusing exhaustion without wrapping.
    /// </summary>
    /// <returns>The next revision or revision exhaustion.</returns>
    public Result<UniqueConstraintAuthorityRevision, UniqueConstraintWriteFailure> Next() => Value == long.MaxValue
        ? Result<UniqueConstraintAuthorityRevision, UniqueConstraintWriteFailure>.Failed(UniqueConstraintWriteFailure.RevisionExhausted)
        : Result<UniqueConstraintAuthorityRevision, UniqueConstraintWriteFailure>.Success(new(checked(Value + 1)));
}
