// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions;

/// <summary>Exclusive ownership of completing the request's unit of work.</summary>
public interface IUnitOfWorkCompletionClaim
{
    /// <summary>Gets the claimed unit of work.</summary>
    IUnitOfWork UnitOfWork { get; }

    /// <summary>Gets the completion result, or <see langword="null"/> until the claim has completed the unit.</summary>
    UnitOfWorkCompletionResult? Result { get; }

    /// <summary>Commits the unit. Idempotent: once completed, returns the same <see cref="Result"/>. Append outcomes and technical failures are reported in the result, not thrown.</summary>
    /// <returns>The <see cref="UnitOfWorkCompletionResult"/>.</returns>
    Task<UnitOfWorkCompletionResult> Commit();

    /// <summary>Rolls the unit back. Once completed, returns the existing <see cref="Result"/> unchanged.</summary>
    /// <returns>The <see cref="UnitOfWorkCompletionResult"/>.</returns>
    Task<UnitOfWorkCompletionResult> Rollback();
}
