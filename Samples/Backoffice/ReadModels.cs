// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Projections.ModelBound;

namespace Samples.Backoffice;

/// <summary>
/// Represents where an invoice has got to.
/// </summary>
public enum InvoiceStatus
{
    /// <summary>Entered, waiting to be matched.</summary>
    Registered = 0,

    /// <summary>Tied to the order it belongs to.</summary>
    Matched = 1,

    /// <summary>Queried with the supplier.</summary>
    Disputed = 2,

    /// <summary>Cleared to be paid.</summary>
    ApprovedForPayment = 3
}

/// <summary>
/// Represents what became of somebody's request for time off.
/// </summary>
public enum LeaveStatus
{
    /// <summary>Asked for, not yet decided.</summary>
    Requested = 0,

    /// <summary>Granted.</summary>
    Approved = 1,

    /// <summary>Turned down.</summary>
    Declined = 2
}

/// <summary>
/// The current state of a supplier invoice.
/// </summary>
/// <param name="Clerk">Who entered it.</param>
/// <param name="Supplier">Who sent it.</param>
/// <param name="Amount">What it is for.</param>
/// <param name="Number">The supplier's own reference.</param>
/// <param name="Status">Where it has got to.</param>
/// <param name="Reason">Why it was disputed.</param>
[FromEvent<InvoiceRegistered>]
[FromEvent<InvoiceMatchedToPurchaseOrder>]
[FromEvent<InvoiceDisputed>]
[FromEvent<InvoiceApprovedForPayment>]
public record Invoice(
    PersonName Clerk,
    SupplierName Supplier,
    Money Amount,
    Reference Number,
    [property: SetValue<InvoiceMatchedToPurchaseOrder>(InvoiceStatus.Matched)]
    [property: SetValue<InvoiceDisputed>(InvoiceStatus.Disputed)]
    [property: SetValue<InvoiceApprovedForPayment>(InvoiceStatus.ApprovedForPayment)]
    InvoiceStatus Status,
    Reason Reason);

/// <summary>
/// The current state of a request for time off.
/// </summary>
/// <param name="Employee">Who asked.</param>
/// <param name="Days">How many days they asked for.</param>
/// <param name="Status">What became of it.</param>
/// <param name="Approver">Who decided.</param>
/// <param name="Reason">Why it was turned down.</param>
[FromEvent<LeaveRequested>]
[FromEvent<LeaveApproved>]
[FromEvent<LeaveDeclined>]
public record LeaveRequest(
    PersonName Employee,
    Quantity Days,
    [property: SetValue<LeaveApproved>(LeaveStatus.Approved)]
    [property: SetValue<LeaveDeclined>(LeaveStatus.Declined)]
    LeaveStatus Status,

    // Both decision events name the property Approver, so AutoMap writes whichever one arrives - which is
    // exactly "who decided this". An explicit mapping per event would say the same thing at more length.
    PersonName Approver,
    Reason Reason);

/// <summary>
/// What has been posted to an accounting period.
/// </summary>
/// <param name="Posted">The total posted to it.</param>
/// <param name="Entries">How many entries it holds.</param>
/// <param name="Closed">Whether it has been closed off.</param>
/// <remarks>
/// A period accumulates across a whole month of postings from more than one source, which gives the projection
/// query editor something with real depth behind it rather than a model with one event per row.
/// </remarks>
[FromEvent<LedgerEntryPosted>]
[FromEvent<PeriodClosed>]
public record Ledger(
    [property: AddFrom<LedgerEntryPosted>(nameof(LedgerEntryPosted.Amount))]
    [property: NoAutoMap]
    Money Posted,
    [property: Count<LedgerEntryPosted>]
    int Entries,
    [property: SetValue<PeriodClosed>(true)]
    bool Closed);

/// <summary>
/// What each person got through.
/// </summary>
/// <param name="InvoicesRegistered">Invoices they entered.</param>
/// <param name="InvoicesMatched">Invoices they matched to an order.</param>
/// <param name="LeaveDecisions">Requests for time off they granted.</param>
/// <param name="TimesheetsReviewed">Timesheets they checked.</param>
/// <param name="OrdersRaised">Purchase orders they raised.</param>
/// <remarks>
/// Keyed by the person, so the projection query editor has an aggregate to demonstrate. The empty columns are the
/// interesting part: each person's row is sparse in a different place, which is the same thing the patterns say -
/// that these people do genuinely different jobs.
/// </remarks>
[FromEvent<InvoiceRegistered>(nameof(InvoiceRegistered.Clerk))]
[FromEvent<InvoiceMatchedToPurchaseOrder>(nameof(InvoiceMatchedToPurchaseOrder.Clerk))]
[FromEvent<LeaveApproved>(nameof(LeaveApproved.Approver))]
[FromEvent<TimesheetReviewed>(nameof(TimesheetReviewed.Reviewer))]
[FromEvent<PurchaseOrderRaised>(nameof(PurchaseOrderRaised.Buyer))]
public record StaffActivity(
    [property: Count<InvoiceRegistered>]
    int InvoicesRegistered,
    [property: Count<InvoiceMatchedToPurchaseOrder>]
    int InvoicesMatched,
    [property: Count<LeaveApproved>]
    int LeaveDecisions,
    [property: Count<TimesheetReviewed>]
    int TimesheetsReviewed,
    [property: Count<PurchaseOrderRaised>]
    int OrdersRaised);

/// <summary>
/// Lets the sample history be generated exactly once.
/// </summary>
/// <remarks>
/// The generator appends its marker before anything else, so a second run is turned away by this constraint and
/// leaves the store as it found it rather than laying a duplicate history on top of the first.
/// </remarks>
public class UniqueSampleHistory : IConstraint
{
    /// <inheritdoc/>
    public void Define(IConstraintBuilder builder) =>
        builder.Unique<SampleHistoryGenerated>("This store already holds the sample history.");
}
