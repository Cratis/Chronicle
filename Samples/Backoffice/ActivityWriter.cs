// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Samples.Backoffice;

/// <summary>
/// Works out the events one piece of work produces.
/// </summary>
/// <remarks>
/// Work in a back office arrives from a colleague, not from outside: an invoice is matched against an order
/// somebody in procurement raised, a payment is released against an invoice accounts payable entered, a timesheet
/// is checked because somebody handed it in. So where a chain needs something to have happened first, the earlier
/// event is written as the colleague whose job it is, at the time of day they would have done it - not offset from
/// whoever is acting now. That keeps everybody's own pattern clean instead of smearing one person's routine across
/// everybody who happens to depend on it.
/// </remarks>
public static class ActivityWriter
{
    /// <summary>
    /// Works out the events one occurrence of an activity produces.
    /// </summary>
    /// <param name="activity">The <see cref="Activity"/> being carried out.</param>
    /// <param name="actor">The <see cref="Actor"/> carrying it out.</param>
    /// <param name="at">When they did it.</param>
    /// <param name="index">The occurrence's position in the run, which determines the identities it uses.</param>
    /// <param name="random">The seeded <see cref="Random"/> the run varies through.</param>
    /// <returns>The events the activity produces.</returns>
    public static IEnumerable<PlannedEvent> Plan(Activity activity, Actor actor, DateTimeOffset at, int index, Random random) => activity switch
    {
        Activity.InvoiceRegistration => RegisterInvoice(actor, at, index, random),
        Activity.InvoiceMatching => MatchInvoice(actor, at, index, random),
        Activity.InvoiceDispute => DisputeInvoice(actor, at, index, random),
        Activity.PaymentApproval => ApprovePayment(actor, at, index, random),
        Activity.PurchaseOrdering => RaisePurchaseOrder(actor, at, index, random),
        Activity.QuoteApproval => ApproveQuote(actor, at, index, random),
        Activity.SupplierReview => ReviewSupplier(actor, at, index, random),
        Activity.LedgerPosting => PostToLedger(actor, at, random),
        Activity.PeriodClose => ClosePeriod(actor, at),
        Activity.LeaveDecision => DecideLeave(actor, at, index, random),
        Activity.CandidateScreening => ScreenCandidate(actor, at, index, random),
        Activity.TimesheetReview => ReviewTimesheet(actor, at, index, random),
        Activity.PayrollProcessing => RunPayroll(actor, at, index),
        Activity.PayrollQuery => AnswerPayrollQuery(actor, at, index),
        Activity.OwnTimesheet => SubmitOwnTimesheet(actor, at, index, random),
        Activity.OwnLeaveRequest => RequestOwnLeave(actor, at, index, random),
        Activity.LedgerReconciliation => ReconcileLedger(actor, at, random),
        _ => ArchivePeriod(actor, at)
    };

    static SupplierName SupplierFrom(Random random) => Workforce.Suppliers[random.Next(Workforce.Suppliers.Length)];

    static Reason ReasonFrom(Random random) => Workforce.Reasons[random.Next(Workforce.Reasons.Length)];

    static Money AmountFrom(Random random) => new(Math.Round((decimal)(random.NextDouble() * 40000) + 500, 2));

    static Reference PeriodFrom(DateTimeOffset at) => new($"{at.Year}-{at.Month:00}");

    /// <summary>
    /// The moment a colleague would have done their part, on an earlier day at their own time of day.
    /// </summary>
    /// <param name="at">The moment the current actor is working at.</param>
    /// <param name="time">The part of the day the colleague works in.</param>
    /// <param name="random">The seeded <see cref="Random"/> the run varies through.</param>
    /// <returns>The earlier moment.</returns>
    static DateTimeOffset Earlier(DateTimeOffset at, TimeOfDay time, Random random)
    {
        // Walk back to a working day. Back-office work happens on weekdays, and letting the colleague's part land
        // on a Saturday would put a faint weekend smudge on the heatmap of somebody who never works weekends.
        var date = at.AddDays(-random.Next(1, 6));
        while (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            date = date.AddDays(-1);
        }

        return new DateTimeOffset(date.Year, date.Month, date.Day, random.Next(time.FirstHour, time.LastHour + 1), random.Next(0, 50), random.Next(0, 60), TimeSpan.Zero);
    }

    static IEnumerable<PlannedEvent> RegisterInvoice(Actor actor, DateTimeOffset at, int index, Random random)
    {
        InvoiceId invoiceId = Identities.For(index, Identities.InvoiceMarker);
        yield return new PlannedEvent(invoiceId, new InvoiceRegistered(actor.Name, SupplierFrom(random), AmountFrom(random), new Reference($"INV-{100000 + index}")), actor, at, AggregateType.Invoice, Commands.RegisterInvoice);
    }

    static IEnumerable<PlannedEvent> MatchInvoice(Actor actor, DateTimeOffset at, int index, Random random)
    {
        // An invoice tied to the order procurement raised for it.
        var supplier = SupplierFrom(random);
        PurchaseOrderId orderId = Identities.For(index, Identities.PurchaseOrderMarker);
        InvoiceId invoiceId = Identities.For(index + 200_000, Identities.InvoiceMarker);

        yield return new PlannedEvent(orderId, new PurchaseOrderRaised(Workforce.Mira.Name, supplier, AmountFrom(random)), Workforce.Mira, Earlier(at, TimeOfDay.Midday, random), AggregateType.PurchaseOrder, Commands.RaisePurchaseOrder);
        yield return new PlannedEvent(invoiceId, new InvoiceRegistered(actor.Name, supplier, AmountFrom(random), new Reference($"INV-{300000 + index}")), actor, Earlier(at, TimeOfDay.EarlyMorning, random), AggregateType.Invoice, Commands.RegisterInvoice);
        yield return new PlannedEvent(invoiceId, new InvoiceMatchedToPurchaseOrder(actor.Name), actor, at, AggregateType.Invoice, Commands.MatchInvoiceToPurchaseOrder, Commands.RaisePurchaseOrder);
    }

    static IEnumerable<PlannedEvent> DisputeInvoice(Actor actor, DateTimeOffset at, int index, Random random)
    {
        InvoiceId invoiceId = Identities.For(index + 400_000, Identities.InvoiceMarker);
        yield return new PlannedEvent(invoiceId, new InvoiceRegistered(actor.Name, SupplierFrom(random), AmountFrom(random), new Reference($"INV-{500000 + index}")), actor, Earlier(at, TimeOfDay.EarlyMorning, random), AggregateType.Invoice, Commands.RegisterInvoice);
        yield return new PlannedEvent(invoiceId, new InvoiceDisputed(actor.Name, ReasonFrom(random)), actor, at, AggregateType.Invoice, Commands.DisputeInvoice, Commands.RegisterInvoice);
    }

    static IEnumerable<PlannedEvent> ApprovePayment(Actor actor, DateTimeOffset at, int index, Random random)
    {
        // An invoice accounts payable matched, released for payment by the controller.
        InvoiceId invoiceId = Identities.For(index + 600_000, Identities.InvoiceMarker);
        yield return new PlannedEvent(invoiceId, new InvoiceMatchedToPurchaseOrder(Workforce.Ingrid.Name), Workforce.Ingrid, Earlier(at, TimeOfDay.Midday, random), AggregateType.Invoice, Commands.MatchInvoiceToPurchaseOrder);
        yield return new PlannedEvent(invoiceId, new InvoiceApprovedForPayment(actor.Name), actor, at, AggregateType.Invoice, Commands.ApproveInvoiceForPayment, Commands.MatchInvoiceToPurchaseOrder);
    }

    static IEnumerable<PlannedEvent> RaisePurchaseOrder(Actor actor, DateTimeOffset at, int index, Random random)
    {
        PurchaseOrderId orderId = Identities.For(index + 800_000, Identities.PurchaseOrderMarker);
        yield return new PlannedEvent(orderId, new PurchaseOrderRaised(actor.Name, SupplierFrom(random), AmountFrom(random)), actor, at, AggregateType.PurchaseOrder, Commands.RaisePurchaseOrder);
    }

    static IEnumerable<PlannedEvent> ApproveQuote(Actor actor, DateTimeOffset at, int index, Random random)
    {
        // A quote accepted, and the order that follows from accepting it.
        var supplier = SupplierFrom(random);
        var amount = AmountFrom(random);
        PurchaseOrderId orderId = Identities.For(index + 1_000_000, Identities.PurchaseOrderMarker);

        yield return new PlannedEvent(orderId, new QuoteApproved(actor.Name, amount), actor, at, AggregateType.PurchaseOrder, Commands.ApproveQuote);
        yield return new PlannedEvent(orderId, new PurchaseOrderRaised(actor.Name, supplier, amount), actor, at.AddMinutes(random.Next(2, 9)), AggregateType.PurchaseOrder, Commands.RaisePurchaseOrder, Commands.ApproveQuote);
    }

    static IEnumerable<PlannedEvent> ReviewSupplier(Actor actor, DateTimeOffset at, int index, Random random)
    {
        SupplierId supplierId = Identities.For(index % 6, Identities.SupplierMarker);
        yield return new PlannedEvent(supplierId, new SupplierPerformanceReviewed(actor.Name, new Quantity(random.Next(2, 6))), actor, at, AggregateType.Supplier, Commands.ReviewSupplierPerformance);
    }

    static IEnumerable<PlannedEvent> PostToLedger(Actor actor, DateTimeOffset at, Random random)
    {
        LedgerId ledgerId = Identities.For((at.Year * 12) + at.Month, Identities.LedgerMarker);
        yield return new PlannedEvent(ledgerId, new LedgerEntryPosted(actor.Name, AmountFrom(random), Workforce.Accounts[random.Next(Workforce.Accounts.Length)]), actor, at, AggregateType.Ledger, Commands.PostLedgerEntry);
    }

    static IEnumerable<PlannedEvent> ClosePeriod(Actor actor, DateTimeOffset at)
    {
        LedgerId ledgerId = Identities.For((at.Year * 12) + at.Month, Identities.LedgerMarker);
        yield return new PlannedEvent(ledgerId, new PeriodClosed(actor.Name, PeriodFrom(at)), actor, at, AggregateType.Ledger, Commands.ClosePeriod, Commands.PostLedgerEntry);
    }

    static IEnumerable<PlannedEvent> DecideLeave(Actor actor, DateTimeOffset at, int index, Random random)
    {
        // Somebody's request for time off, granted or turned down.
        var employee = Workforce.Everybody[random.Next(Workforce.Everybody.Length)];
        LeaveRequestId leaveId = Identities.For(index + 1_200_000, Identities.LeaveMarker);

        yield return new PlannedEvent(leaveId, new LeaveRequested(employee.Name, new Quantity(random.Next(1, 12))), employee, Earlier(at, TimeOfDay.Evening, random), AggregateType.LeaveRequest, Commands.RequestLeave);

        if (random.Next(100) < 80)
        {
            yield return new PlannedEvent(leaveId, new LeaveApproved(actor.Name), actor, at, AggregateType.LeaveRequest, Commands.ApproveLeave, Commands.RequestLeave);
            yield break;
        }

        yield return new PlannedEvent(leaveId, new LeaveDeclined(actor.Name, ReasonFrom(random)), actor, at, AggregateType.LeaveRequest, Commands.DeclineLeave, Commands.RequestLeave);
    }

    static IEnumerable<PlannedEvent> ScreenCandidate(Actor actor, DateTimeOffset at, int index, Random random)
    {
        // An application read, and either taken further or turned down.
        CandidateId candidateId = Identities.For(index, Identities.CandidateMarker);

        yield return new PlannedEvent(candidateId, new CandidateApplied(Workforce.Positions[random.Next(Workforce.Positions.Length)]), actor, Earlier(at, TimeOfDay.Evening, random), AggregateType.Candidate, Commands.SubmitApplication);
        yield return new PlannedEvent(candidateId, new CandidateScreened(actor.Name), actor, at, AggregateType.Candidate, Commands.ScreenCandidate, Commands.SubmitApplication);

        if (random.Next(100) < 45)
        {
            yield return new PlannedEvent(candidateId, new InterviewScheduled(actor.Name), actor, at.AddMinutes(random.Next(2, 9)), AggregateType.Candidate, Commands.ScheduleInterview, Commands.ScreenCandidate);
            yield break;
        }

        yield return new PlannedEvent(candidateId, new CandidateRejected(actor.Name, ReasonFrom(random)), actor, at.AddMinutes(random.Next(2, 9)), AggregateType.Candidate, Commands.RejectCandidate, Commands.ScreenCandidate);
    }

    static IEnumerable<PlannedEvent> ReviewTimesheet(Actor actor, DateTimeOffset at, int index, Random random)
    {
        // Somebody's hours, checked by payroll.
        var employee = Workforce.Everybody[random.Next(Workforce.Everybody.Length)];
        TimesheetId timesheetId = Identities.For(index + 1_400_000, Identities.TimesheetMarker);

        yield return new PlannedEvent(timesheetId, new TimesheetSubmitted(employee.Name, new Quantity(random.Next(30, 46)), PeriodFrom(at)), employee, Earlier(at, TimeOfDay.Evening, random), AggregateType.Timesheet, Commands.SubmitTimesheet);
        yield return new PlannedEvent(timesheetId, new TimesheetReviewed(actor.Name), actor, at, AggregateType.Timesheet, Commands.ReviewTimesheet, Commands.SubmitTimesheet);
    }

    static IEnumerable<PlannedEvent> RunPayroll(Actor actor, DateTimeOffset at, int index)
    {
        TimesheetId timesheetId = Identities.For(index + 1_600_000, Identities.TimesheetMarker);
        yield return new PlannedEvent(timesheetId, new PayrollRun(actor.Name, PeriodFrom(at)), actor, at, AggregateType.Timesheet, Commands.RunPayroll, Commands.ReviewTimesheet);
    }

    static IEnumerable<PlannedEvent> AnswerPayrollQuery(Actor actor, DateTimeOffset at, int index)
    {
        TimesheetId timesheetId = Identities.For(index + 1_800_000, Identities.TimesheetMarker);
        yield return new PlannedEvent(timesheetId, new PayrollQueryAnswered(actor.Name), actor, at, AggregateType.Timesheet, Commands.AnswerPayrollQuery);
    }

    static IEnumerable<PlannedEvent> SubmitOwnTimesheet(Actor actor, DateTimeOffset at, int index, Random random)
    {
        // Handing in your own hours - the ordinary employee work everybody does on top of their job.
        TimesheetId timesheetId = Identities.For(index + 2_000_000, Identities.TimesheetMarker);
        yield return new PlannedEvent(timesheetId, new TimesheetSubmitted(actor.Name, new Quantity(random.Next(30, 46)), PeriodFrom(at)), actor, at, AggregateType.Timesheet, Commands.SubmitTimesheet);
    }

    static IEnumerable<PlannedEvent> RequestOwnLeave(Actor actor, DateTimeOffset at, int index, Random random)
    {
        LeaveRequestId leaveId = Identities.For(index + 2_200_000, Identities.LeaveMarker);
        yield return new PlannedEvent(leaveId, new LeaveRequested(actor.Name, new Quantity(random.Next(1, 12))), actor, at, AggregateType.LeaveRequest, Commands.RequestLeave);
    }

    static IEnumerable<PlannedEvent> ReconcileLedger(Actor actor, DateTimeOffset at, Random random)
    {
        LedgerId ledgerId = Identities.For((at.Year * 12) + at.Month, Identities.LedgerMarker);
        yield return new PlannedEvent(ledgerId, new LedgerReconciled(new Quantity(random.Next(20, 200))), actor, at, AggregateType.Ledger, Commands.ReconcileLedger, Commands.PostLedgerEntry);
    }

    static IEnumerable<PlannedEvent> ArchivePeriod(Actor actor, DateTimeOffset at)
    {
        LedgerId ledgerId = Identities.For((at.Year * 12) + at.Month - 1, Identities.LedgerMarker);
        yield return new PlannedEvent(ledgerId, new PeriodArchived(PeriodFrom(at.AddMonths(-1))), actor, at, AggregateType.Ledger, Commands.ArchivePeriod, Commands.ClosePeriod);
    }
}
