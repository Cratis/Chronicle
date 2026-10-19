// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Transactions;

/// <summary>The result of completing a unit of work through an <see cref="IUnitOfWorkCompletionClaim"/>.</summary>
/// <param name="Outcome">The <see cref="UnitOfWorkCompletionOutcome"/>.</param>
/// <param name="CorrelationId">The <see cref="CorrelationId"/> of the unit of work.</param>
/// <param name="ConcurrencyViolations">The concurrency violations reported by the commit.</param>
/// <param name="DecisionConflicts">The decision-read conflicts reported by the commit.</param>
/// <param name="ConstraintViolations">The constraint violations reported by the commit.</param>
/// <param name="Errors">The append errors reported by the commit. A <see cref="UnitOfWorkCompletionOutcome.Failed"/> result may carry errors and no <see cref="Exception"/>.</param>
/// <param name="Exception">The technical failure for <see cref="UnitOfWorkCompletionOutcome.Unknown"/>, otherwise <see langword="null"/>.</param>
/// <remarks>
/// A <see cref="DecisionConflict"/> is derived from a <see cref="ConcurrencyViolation"/>: every decision conflict
/// is caused by a concurrency violation for the same event source, so those violations are reported in both
/// <see cref="ConcurrencyViolations"/> and <see cref="DecisionConflicts"/>. A caller that maps both should drop
/// each violation whose event source id equals a decision conflict's key.
/// </remarks>
public sealed record UnitOfWorkCompletionResult(
    UnitOfWorkCompletionOutcome Outcome,
    CorrelationId CorrelationId,
    IReadOnlyList<ConcurrencyViolation> ConcurrencyViolations,
    IReadOnlyList<DecisionConflict> DecisionConflicts,
    IReadOnlyList<ConstraintViolation> ConstraintViolations,
    IReadOnlyList<AppendError> Errors,
    Exception? Exception = null)
{
    /// <summary>Gets a value indicating whether the unit of work was committed.</summary>
    public bool IsSuccess => Outcome == UnitOfWorkCompletionOutcome.Committed;

    /// <summary>Gets a value indicating whether there are concurrency violations or decision conflicts.</summary>
    public bool HasConflicts => ConcurrencyViolations.Count > 0 || DecisionConflicts.Count > 0;

    /// <summary>Gets a value indicating whether there are constraint violations.</summary>
    public bool HasConstraintViolations => ConstraintViolations.Count > 0;
}
