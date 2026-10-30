// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.AspNetCore.Transactions;

/// <summary>Builds <see cref="UnitOfWorkCompletionResult"/> instances from a unit of work.</summary>
internal static class UnitOfWorkCompletionResults
{
    /// <summary>Builds the result of a commit that returned normally.</summary>
    /// <param name="unitOfWork">The completed unit.</param>
    /// <returns>The result.</returns>
    internal static UnitOfWorkCompletionResult FromCommit(IUnitOfWork unitOfWork) => new(
        unitOfWork.IsSuccess ? UnitOfWorkCompletionOutcome.Committed : UnitOfWorkCompletionOutcome.Failed,
        unitOfWork.CorrelationId,
        [.. unitOfWork.GetConcurrencyViolations()],
        [.. unitOfWork.GetDecisionConflicts()],
        [.. unitOfWork.GetConstraintViolations()],
        [.. unitOfWork.GetAppendErrors()]);

    /// <summary>Builds the result of a commit that failed technically; <c language="csharp">IsSuccess</c> is not trusted.</summary>
    /// <param name="unitOfWork">The unit.</param>
    /// <param name="exception">The failure.</param>
    /// <returns>The result.</returns>
    internal static UnitOfWorkCompletionResult FromUnknown(IUnitOfWork unitOfWork, Exception exception) => Empty(unitOfWork, UnitOfWorkCompletionOutcome.Unknown, exception);

    /// <summary>Builds the result of a rollback.</summary>
    /// <param name="unitOfWork">The unit.</param>
    /// <returns>The result.</returns>
    internal static UnitOfWorkCompletionResult FromRollback(IUnitOfWork unitOfWork) => Empty(unitOfWork, UnitOfWorkCompletionOutcome.RolledBack, null);

    /// <summary>Builds the result of a unit that was completed outside the completion feature.</summary>
    /// <param name="unitOfWork">The already completed unit.</param>
    /// <returns>The result.</returns>
    /// <remarks>A commit that threw completed the unit without a trustworthy result, so it is reported as unknown.</remarks>
    internal static UnitOfWorkCompletionResult FromCompleted(IUnitOfWork unitOfWork) => unitOfWork switch
    {
        UnitOfWork concrete when concrete.WasRolledBack() => FromRollback(unitOfWork),
        UnitOfWork concrete when concrete.CommitFailure() is { } failure => FromUnknown(unitOfWork, failure),
        _ => FromCommit(unitOfWork)
    };

    static UnitOfWorkCompletionResult Empty(IUnitOfWork unitOfWork, UnitOfWorkCompletionOutcome outcome, Exception? exception) => new(
        outcome,
        unitOfWork.CorrelationId,
        [],
        [],
        [],
        [],
        exception);
}
