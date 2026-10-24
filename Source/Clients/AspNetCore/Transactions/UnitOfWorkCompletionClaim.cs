// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.AspNetCore.Transactions;

/// <summary>The <see cref="IUnitOfWorkCompletionClaim"/> handed out by the completion feature.</summary>
/// <param name="unitOfWork">The claimed unit of work.</param>
/// <param name="commit">Commits through the feature's completion state machine.</param>
/// <param name="rollback">Rolls back through the feature's completion state machine.</param>
/// <param name="result">Gets the completion result, if completed.</param>
internal sealed class UnitOfWorkCompletionClaim(
    IUnitOfWork unitOfWork,
    Func<Task<UnitOfWorkCompletionResult>> commit,
    Func<Task<UnitOfWorkCompletionResult>> rollback,
    Func<UnitOfWorkCompletionResult?> result) : IUnitOfWorkCompletionClaim
{
    /// <inheritdoc/>
    public IUnitOfWork UnitOfWork => unitOfWork;

    /// <inheritdoc/>
    public UnitOfWorkCompletionResult? Result => result();

    /// <inheritdoc/>
    public Task<UnitOfWorkCompletionResult> Commit() => commit();

    /// <inheritdoc/>
    public Task<UnitOfWorkCompletionResult> Rollback() => rollback();
}
