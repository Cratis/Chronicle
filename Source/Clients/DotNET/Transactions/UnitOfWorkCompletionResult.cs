// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Transactions;

/// <summary>The result of completing a unit of work through an <see cref="IUnitOfWorkCompletionClaim"/>.</summary>
/// <remarks>
/// <para>
/// Only Chronicle creates results; callers read them. <see cref="UnitOfWorkCompletionOutcome.Unknown"/> means the
/// commit failed technically: the events may or may not have been appended, so a caller must never report it as
/// rolled back. <see cref="UnitOfWorkCompletionOutcome.Failed"/> means Chronicle refused the commit and reported why in the violations and errors.
/// </para>
/// <para>
/// A <see cref="DecisionConflict"/> is derived from a <see cref="ConcurrencyViolation"/>: every decision conflict
/// is caused by a concurrency violation for the same event source, so those violations are reported in both
/// <see cref="ConcurrencyViolations"/> and <see cref="DecisionConflicts"/>. A caller that maps both should drop
/// each violation whose event source id equals a decision conflict's key.
/// </para>
/// </remarks>
public sealed class UnitOfWorkCompletionResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnitOfWorkCompletionResult"/> class.
    /// </summary>
    /// <param name="outcome">The <see cref="UnitOfWorkCompletionOutcome"/>.</param>
    /// <param name="correlationId">The <see cref="CorrelationId"/> of the unit of work.</param>
    /// <param name="concurrencyViolations">The concurrency violations reported by the commit.</param>
    /// <param name="decisionConflicts">The decision-read conflicts reported by the commit.</param>
    /// <param name="constraintViolations">The constraint violations reported by the commit.</param>
    /// <param name="errors">The append errors reported by the commit.</param>
    /// <param name="exception">The technical failure for <see cref="UnitOfWorkCompletionOutcome.Unknown"/>, otherwise <see langword="null"/>.</param>
    internal UnitOfWorkCompletionResult(
        UnitOfWorkCompletionOutcome outcome,
        CorrelationId correlationId,
        IReadOnlyList<ConcurrencyViolation> concurrencyViolations,
        IReadOnlyList<DecisionConflict> decisionConflicts,
        IReadOnlyList<ConstraintViolation> constraintViolations,
        IReadOnlyList<AppendError> errors,
        Exception? exception = null)
    {
        Outcome = outcome;
        CorrelationId = correlationId;
        ConcurrencyViolations = concurrencyViolations;
        DecisionConflicts = decisionConflicts;
        ConstraintViolations = constraintViolations;
        Errors = errors;
        Exception = exception;
    }

    /// <summary>Gets the <see cref="UnitOfWorkCompletionOutcome"/>.</summary>
    public UnitOfWorkCompletionOutcome Outcome { get; }

    /// <summary>Gets the <see cref="CorrelationId"/> of the unit of work.</summary>
    public CorrelationId CorrelationId { get; }

    /// <summary>Gets the concurrency violations reported by the commit.</summary>
    public IReadOnlyList<ConcurrencyViolation> ConcurrencyViolations { get; }

    /// <summary>Gets the decision-read conflicts reported by the commit.</summary>
    public IReadOnlyList<DecisionConflict> DecisionConflicts { get; }

    /// <summary>Gets the constraint violations reported by the commit.</summary>
    public IReadOnlyList<ConstraintViolation> ConstraintViolations { get; }

    /// <summary>Gets the append errors reported by the commit. A <see cref="UnitOfWorkCompletionOutcome.Failed"/> result may carry errors and no <see cref="Exception"/>.</summary>
    public IReadOnlyList<AppendError> Errors { get; }

    /// <summary>Gets the technical failure for <see cref="UnitOfWorkCompletionOutcome.Unknown"/>, otherwise <see langword="null"/>.</summary>
    public Exception? Exception { get; }

    /// <summary>Gets a value indicating whether the unit of work was committed.</summary>
    public bool IsSuccess => Outcome == UnitOfWorkCompletionOutcome.Committed;

    /// <summary>Gets a value indicating whether there are concurrency violations or decision conflicts.</summary>
    public bool HasConflicts => ConcurrencyViolations.Count > 0 || DecisionConflicts.Count > 0;

    /// <summary>Gets a value indicating whether there are constraint violations.</summary>
    public bool HasConstraintViolations => ConstraintViolations.Count > 0;
}
