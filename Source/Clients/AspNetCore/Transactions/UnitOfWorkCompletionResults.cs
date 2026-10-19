// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.ReadModels;
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

    /// <summary>Builds the result of a commit that failed technically; <c>IsSuccess</c> is not trusted.</summary>
    /// <param name="unitOfWork">The unit.</param>
    /// <param name="exception">The failure.</param>
    /// <returns>The result.</returns>
    internal static UnitOfWorkCompletionResult FromUnknown(IUnitOfWork unitOfWork, Exception exception) => Empty(unitOfWork, UnitOfWorkCompletionOutcome.Unknown, exception);

    /// <summary>Builds the result of a rollback.</summary>
    /// <param name="unitOfWork">The unit.</param>
    /// <returns>The result.</returns>
    internal static UnitOfWorkCompletionResult FromRollback(IUnitOfWork unitOfWork) => Empty(unitOfWork, UnitOfWorkCompletionOutcome.RolledBack, null);

    static UnitOfWorkCompletionResult Empty(IUnitOfWork unitOfWork, UnitOfWorkCompletionOutcome outcome, Exception? exception) => new(
        outcome,
        unitOfWork.CorrelationId,
        ImmutableList<ConcurrencyViolation>.Empty,
        ImmutableList<DecisionConflict>.Empty,
        ImmutableList<ConstraintViolation>.Empty,
        ImmutableList<AppendError>.Empty,
        exception);
}
