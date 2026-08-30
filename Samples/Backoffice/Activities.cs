// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Samples.Backoffice;

/// <summary>
/// The pieces of work somebody in the back office does.
/// </summary>
/// <remarks>
/// Nobody here does only one of these. A person with one task establishes one pattern, and a set of people who each
/// establish one pattern is not worth browsing - so every one of them holds three or four of these at different
/// points in the week.
/// </remarks>
public enum Activity
{
    /// <summary>Entering supplier invoices as they arrive.</summary>
    InvoiceRegistration = 0,

    /// <summary>Tying invoices to the orders they belong to.</summary>
    InvoiceMatching = 1,

    /// <summary>Querying the invoices that do not add up.</summary>
    InvoiceDispute = 2,

    /// <summary>Releasing approved invoices for payment.</summary>
    PaymentApproval = 3,

    /// <summary>Committing to buy something.</summary>
    PurchaseOrdering = 4,

    /// <summary>Accepting what a supplier quoted.</summary>
    QuoteApproval = 5,

    /// <summary>Recording how a supplier has been doing.</summary>
    SupplierReview = 6,

    /// <summary>Posting amounts to the ledger.</summary>
    LedgerPosting = 7,

    /// <summary>Closing an accounting period off.</summary>
    PeriodClose = 8,

    /// <summary>Deciding on somebody's request for time off.</summary>
    LeaveDecision = 9,

    /// <summary>Reading applications and taking the good ones further.</summary>
    CandidateScreening = 10,

    /// <summary>Checking somebody's hours.</summary>
    TimesheetReview = 11,

    /// <summary>Running the pay for a period.</summary>
    PayrollProcessing = 12,

    /// <summary>Answering somebody's question about their pay.</summary>
    PayrollQuery = 13,

    /// <summary>Handing in your own hours - something everybody does.</summary>
    OwnTimesheet = 14,

    /// <summary>Asking for time off - something everybody does.</summary>
    OwnLeaveRequest = 15,

    /// <summary>Squaring the ledger off overnight.</summary>
    LedgerReconciliation = 16,

    /// <summary>Filing a closed period away.</summary>
    PeriodArchive = 17
}

/// <summary>
/// The command names the generated history is recorded under.
/// </summary>
/// <remarks>
/// These are what the miner reads as the action, so they are the vocabulary the patterns end up being about.
/// </remarks>
public static class Commands
{
    /// <summary>Entering a supplier invoice.</summary>
    public const string RegisterInvoice = nameof(RegisterInvoice);

    /// <summary>Tying an invoice to its order.</summary>
    public const string MatchInvoiceToPurchaseOrder = nameof(MatchInvoiceToPurchaseOrder);

    /// <summary>Querying an invoice with the supplier.</summary>
    public const string DisputeInvoice = nameof(DisputeInvoice);

    /// <summary>Clearing an invoice to be paid.</summary>
    public const string ApproveInvoiceForPayment = nameof(ApproveInvoiceForPayment);

    /// <summary>Committing to a purchase.</summary>
    public const string RaisePurchaseOrder = nameof(RaisePurchaseOrder);

    /// <summary>Accepting a supplier's quote.</summary>
    public const string ApproveQuote = nameof(ApproveQuote);

    /// <summary>Recording how a supplier is doing.</summary>
    public const string ReviewSupplierPerformance = nameof(ReviewSupplierPerformance);

    /// <summary>Posting to the ledger.</summary>
    public const string PostLedgerEntry = nameof(PostLedgerEntry);

    /// <summary>Closing an accounting period.</summary>
    public const string ClosePeriod = nameof(ClosePeriod);

    /// <summary>Squaring the ledger off against the bank.</summary>
    public const string ReconcileLedger = nameof(ReconcileLedger);

    /// <summary>Filing a closed period away.</summary>
    public const string ArchivePeriod = nameof(ArchivePeriod);

    /// <summary>Asking for time off.</summary>
    public const string RequestLeave = nameof(RequestLeave);

    /// <summary>Granting time off.</summary>
    public const string ApproveLeave = nameof(ApproveLeave);

    /// <summary>Turning down a request for time off.</summary>
    public const string DeclineLeave = nameof(DeclineLeave);

    /// <summary>Somebody applying for a job.</summary>
    public const string SubmitApplication = nameof(SubmitApplication);

    /// <summary>Reading an application.</summary>
    public const string ScreenCandidate = nameof(ScreenCandidate);

    /// <summary>Inviting a candidate in.</summary>
    public const string ScheduleInterview = nameof(ScheduleInterview);

    /// <summary>Turning a candidate down.</summary>
    public const string RejectCandidate = nameof(RejectCandidate);

    /// <summary>Handing in hours.</summary>
    public const string SubmitTimesheet = nameof(SubmitTimesheet);

    /// <summary>Checking somebody's hours.</summary>
    public const string ReviewTimesheet = nameof(ReviewTimesheet);

    /// <summary>Running the pay.</summary>
    public const string RunPayroll = nameof(RunPayroll);

    /// <summary>Answering a question about pay.</summary>
    public const string AnswerPayrollQuery = nameof(AnswerPayrollQuery);

    /// <summary>Recording that the sample history was generated.</summary>
    public const string GenerateSampleHistory = nameof(GenerateSampleHistory);
}

/// <summary>
/// Builds the stable identities the generated history uses.
/// </summary>
/// <remarks>
/// Derived from a position rather than generated, so a re-run addresses the same event sources. The marker byte
/// keeps the different kinds of thing from colliding with each other.
/// </remarks>
public static class Identities
{
    /// <summary>The marker for an invoice.</summary>
    public const byte InvoiceMarker = 0xB1;

    /// <summary>The marker for a purchase order.</summary>
    public const byte PurchaseOrderMarker = 0xB2;

    /// <summary>The marker for a supplier.</summary>
    public const byte SupplierMarker = 0xB3;

    /// <summary>The marker for a ledger period.</summary>
    public const byte LedgerMarker = 0xB4;

    /// <summary>The marker for a request for time off.</summary>
    public const byte LeaveMarker = 0xB5;

    /// <summary>The marker for a candidate.</summary>
    public const byte CandidateMarker = 0xB6;

    /// <summary>The marker for a timesheet.</summary>
    public const byte TimesheetMarker = 0xB7;

    /// <summary>
    /// Builds an identity from a position and a kind.
    /// </summary>
    /// <param name="index">The position in the run.</param>
    /// <param name="marker">The byte identifying the kind of thing.</param>
    /// <returns>The <see cref="Guid"/> to use as the event source id.</returns>
    public static Guid For(int index, byte marker)
    {
        var bytes = new byte[16];
        BitConverter.TryWriteBytes(bytes, index);
        bytes[15] = marker;
        return new Guid(bytes);
    }
}
