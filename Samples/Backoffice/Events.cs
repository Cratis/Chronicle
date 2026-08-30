// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Samples.Backoffice;

/// <summary>
/// Emitted when a supplier invoice is entered into the system.
/// </summary>
/// <param name="Clerk">Who entered it.</param>
/// <param name="Supplier">Who sent it.</param>
/// <param name="Amount">What it is for.</param>
/// <param name="Number">The supplier's own reference for it.</param>
[EventType]
public record InvoiceRegistered(PersonName Clerk, SupplierName Supplier, Money Amount, Reference Number);

/// <summary>
/// Emitted when an invoice is tied to the purchase order it belongs to.
/// </summary>
/// <param name="Clerk">Who matched it.</param>
[EventType]
public record InvoiceMatchedToPurchaseOrder(PersonName Clerk);

/// <summary>
/// Emitted when an invoice is queried with the supplier rather than paid.
/// </summary>
/// <param name="Clerk">Who raised the dispute.</param>
/// <param name="Reason">What is wrong with it.</param>
[EventType]
public record InvoiceDisputed(PersonName Clerk, Reason Reason);

/// <summary>
/// Emitted when an invoice is cleared to be paid.
/// </summary>
/// <param name="Approver">Who approved it.</param>
[EventType]
public record InvoiceApprovedForPayment(PersonName Approver);

/// <summary>
/// Emitted when somebody commits to buying something.
/// </summary>
/// <param name="Buyer">Who raised it.</param>
/// <param name="Supplier">Who it is with.</param>
/// <param name="Amount">What it commits us to.</param>
[EventType]
public record PurchaseOrderRaised(PersonName Buyer, SupplierName Supplier, Money Amount);

/// <summary>
/// Emitted when a supplier's quote is accepted.
/// </summary>
/// <param name="Buyer">Who accepted it.</param>
/// <param name="Amount">What was quoted.</param>
[EventType]
public record QuoteApproved(PersonName Buyer, Money Amount);

/// <summary>
/// Emitted when somebody records how a supplier has been performing.
/// </summary>
/// <param name="Reviewer">Who reviewed them.</param>
/// <param name="Score">How they scored, out of five.</param>
[EventType]
public record SupplierPerformanceReviewed(PersonName Reviewer, Quantity Score);

/// <summary>
/// Emitted when an amount is posted to the ledger.
/// </summary>
/// <param name="Accountant">Who posted it.</param>
/// <param name="Amount">What was posted.</param>
/// <param name="Account">Which account it went to.</param>
[EventType]
public record LedgerEntryPosted(PersonName Accountant, Money Amount, Reference Account);

/// <summary>
/// Emitted when an accounting period is closed off.
/// </summary>
/// <param name="Accountant">Who closed it.</param>
/// <param name="Period">Which period was closed.</param>
[EventType]
public record PeriodClosed(PersonName Accountant, Reference Period);

/// <summary>
/// Emitted when the overnight run squares the ledger off against the bank.
/// </summary>
/// <param name="Entries">How many entries were reconciled.</param>
[EventType]
public record LedgerReconciled(Quantity Entries);

/// <summary>
/// Emitted when the weekly run files a closed period away.
/// </summary>
/// <param name="Period">Which period was archived.</param>
[EventType]
public record PeriodArchived(Reference Period);

/// <summary>
/// Emitted when somebody asks for time off.
/// </summary>
/// <param name="Employee">Who is asking.</param>
/// <param name="Days">How many days they want.</param>
[EventType]
public record LeaveRequested(PersonName Employee, Quantity Days);

/// <summary>
/// Emitted when a request for time off is granted.
/// </summary>
/// <param name="Approver">Who granted it.</param>
[EventType]
public record LeaveApproved(PersonName Approver);

/// <summary>
/// Emitted when a request for time off is turned down.
/// </summary>
/// <param name="Approver">Who turned it down.</param>
/// <param name="Reason">Why it was turned down.</param>
[EventType]
public record LeaveDeclined(PersonName Approver, Reason Reason);

/// <summary>
/// Emitted when somebody applies for a job.
/// </summary>
/// <param name="Position">What they applied for.</param>
[EventType]
public record CandidateApplied(Reference Position);

/// <summary>
/// Emitted when an application has been read and judged worth taking further.
/// </summary>
/// <param name="Screener">Who read it.</param>
[EventType]
public record CandidateScreened(PersonName Screener);

/// <summary>
/// Emitted when a candidate is invited in.
/// </summary>
/// <param name="Screener">Who arranged it.</param>
[EventType]
public record InterviewScheduled(PersonName Screener);

/// <summary>
/// Emitted when a candidate is turned down.
/// </summary>
/// <param name="Screener">Who turned them down.</param>
/// <param name="Reason">Why.</param>
[EventType]
public record CandidateRejected(PersonName Screener, Reason Reason);

/// <summary>
/// Emitted when somebody hands in their hours for a period.
/// </summary>
/// <param name="Employee">Whose hours they are.</param>
/// <param name="Hours">How many hours.</param>
/// <param name="Period">Which period they cover.</param>
[EventType]
public record TimesheetSubmitted(PersonName Employee, Quantity Hours, Reference Period);

/// <summary>
/// Emitted when somebody's hours have been checked.
/// </summary>
/// <param name="Reviewer">Who checked them.</param>
[EventType]
public record TimesheetReviewed(PersonName Reviewer);

/// <summary>
/// Emitted when the pay run goes through for a period.
/// </summary>
/// <param name="Officer">Who ran it.</param>
/// <param name="Period">Which period was paid.</param>
[EventType]
public record PayrollRun(PersonName Officer, Reference Period);

/// <summary>
/// Emitted when somebody's question about their pay is answered.
/// </summary>
/// <param name="Officer">Who answered it.</param>
[EventType]
public record PayrollQueryAnswered(PersonName Officer);

/// <summary>
/// Emitted once when the sample history has been generated.
/// </summary>
/// <param name="Seed">The seed the run used, so a store says which history it holds.</param>
/// <remarks>
/// This is what makes the generator safe to re-run. A uniqueness constraint means the store accepts it once, so a
/// second run is turned away before it appends anything rather than laying a duplicate history on top of the first.
/// </remarks>
[EventType]
public record SampleHistoryGenerated(Quantity Seed);
