// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Projections.ModelBound;

namespace Samples.ExpenseApprovals;

/// <summary>
/// Represents where an expense report has got to.
/// </summary>
public enum ExpenseReportStatus
{
    /// <summary>
    /// Submitted and waiting on a decision.
    /// </summary>
    Submitted = 0,

    /// <summary>
    /// Passed to somebody with more authority to decide.
    /// </summary>
    Escalated = 1,

    /// <summary>
    /// Approved for reimbursement.
    /// </summary>
    Approved = 2,

    /// <summary>
    /// Turned down.
    /// </summary>
    Rejected = 3,

    /// <summary>
    /// Approved and paid out.
    /// </summary>
    Reimbursed = 4
}

/// <summary>
/// The current state of an expense report.
/// </summary>
/// <param name="Submitter">The person who submitted it.</param>
/// <param name="Amount">The amount claimed.</param>
/// <param name="Category">What the expense was for.</param>
/// <param name="Status">Where the report has got to.</param>
/// <param name="Decider">The person who last acted on it.</param>
/// <param name="Reason">Why it was turned down or escalated.</param>
/// <param name="Reimbursed">The amount actually paid out.</param>
/// <remarks>
/// <see cref="Status"/> carries an initial value because a report genuinely starts out submitted - that is the
/// meaning of the event that creates it, not a stand-in for a projection that has not run.
/// <para>
/// <see cref="Decider"/> is deliberately fed by three different events, because "who last acted on this" is one
/// fact regardless of which way they decided. Each of those events names the person differently, so each needs
/// its own <see cref="SetFromAttribute{TEvent}"/>.
/// </para>
/// </remarks>
[FromEvent<ExpenseReportSubmitted>]
[FromEvent<ExpenseReportApproved>]
[FromEvent<ExpenseReportRejected>]
[FromEvent<ExpenseReportEscalated>]
[FromEvent<ExpenseReportReimbursed>]
public record ExpenseReport(
    PersonName Submitter,
    Money Amount,
    ExpenseCategory Category,
    [property: SetValue<ExpenseReportEscalated>(ExpenseReportStatus.Escalated)]
    [property: SetValue<ExpenseReportApproved>(ExpenseReportStatus.Approved)]
    [property: SetValue<ExpenseReportRejected>(ExpenseReportStatus.Rejected)]
    [property: SetValue<ExpenseReportReimbursed>(ExpenseReportStatus.Reimbursed)]
    ExpenseReportStatus Status,
    [property: SetFrom<ExpenseReportApproved>(nameof(ExpenseReportApproved.Approver))]
    [property: SetFrom<ExpenseReportRejected>(nameof(ExpenseReportRejected.Approver))]
    [property: SetFrom<ExpenseReportEscalated>(nameof(ExpenseReportEscalated.EscalatedTo))]
    PersonName Decider,
    Reason Reason,
    [property: SetFrom<ExpenseReportReimbursed>(nameof(ExpenseReportReimbursed.Amount))]
    [property: NoAutoMap]
    Money Reimbursed);

/// <summary>
/// What each person has claimed across all the reports they submitted.
/// </summary>
/// <param name="Submitted">The number of reports submitted.</param>
/// <param name="Claimed">The total amount claimed across them.</param>
/// <remarks>
/// Keyed by the submitter rather than by the report, so the projection query editor has something to demonstrate
/// beyond a one-event-source-per-model shape. It deliberately counts only what the submission event itself
/// carries - the decision events name the approver, not the submitter, so counting outcomes per submitter would
/// need a join and would stop being the simple aggregate this is here to show.
/// </remarks>
[FromEvent<ExpenseReportSubmitted>(nameof(ExpenseReportSubmitted.Submitter))]
public record SubmitterActivity(
    [property: Count<ExpenseReportSubmitted>]
    int Submitted,
    [property: AddFrom<ExpenseReportSubmitted>(nameof(ExpenseReportSubmitted.Amount))]
    [property: NoAutoMap]
    Money Claimed);

/// <summary>
/// Prevents a report from being submitted more than once.
/// </summary>
/// <remarks>
/// This is what makes the sample data generator re-runnable: a second run replays the same deterministic report
/// ids, the submission is rejected for every report that already exists, and the generator skips the rest of that
/// report's history rather than duplicating it.
/// </remarks>
public class UniqueExpenseReportSubmission : IConstraint
{
    /// <inheritdoc/>
    public void Define(IConstraintBuilder builder) =>
        builder.Unique<ExpenseReportSubmitted>("This expense report has already been submitted.");
}
