// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Identities;

namespace Samples.ExpenseApprovals;

/// <summary>
/// Describes what a generation run produced.
/// </summary>
/// <param name="Reports">The number of reports whose history was appended.</param>
/// <param name="Events">The number of events appended.</param>
/// <param name="Skipped">The number of reports that already existed and were left alone.</param>
public record SampleHistoryResult(int Reports, int Events, int Skipped);

/// <summary>
/// Generates a backdated history of expense activity with recurring behavior deliberately baked into it.
/// </summary>
/// <remarks>
/// The point of the data is not volume, it is that some of it is habitual and some of it is not. Dana, Nina, Victor
/// and the nightly reimbursement run each act at a consistent time; Sam acts at no consistent time and with no
/// consistent decision. A miner worth trusting finds the first four and finds nothing for Sam, so the sample is as
/// much a demonstration of what is not a pattern as of what is.
/// <para>
/// Every event is appended with an explicit <c>occurred</c>, because the whole history happened before the store
/// was ever run. Letting the server stamp append time would collapse months of behavior into the few minutes the
/// generator takes, and every mined pattern would be about the afternoon somebody ran this.
/// </para>
/// </remarks>
public static class SampleHistory
{
    const string Submit = "SubmitExpenseReport";
    const string Approve = "ApproveExpenseReport";
    const string Reject = "RejectExpenseReport";
    const string Escalate = "EscalateExpenseReport";
    const string Reimburse = "ReimburseExpenseReport";

    static readonly DayOfWeek[] _weekdays = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday];
    static readonly DayOfWeek[] _everyDay = [.. Enum.GetValues<DayOfWeek>()];

    static readonly Habit[] _habits =
    [

        // Dana clears the weekend's backlog first thing on a Monday. The strongest habit in the set.
        new(Workforce.Dana, [Approve], [DayOfWeek.Monday], [TimeOfDay.Morning], ClaimType.Expense, 10),

        // Nina works through the queue over lunch, every weekday.
        new(Workforce.Nina, [Approve], _weekdays, [TimeOfDay.Midday], ClaimType.Expense, 4),

        // Victor is the one who says no, and he does it late on a Friday - travel claims especially.
        new(Workforce.Victor, [Reject], [DayOfWeek.Friday], [TimeOfDay.Evening], ClaimType.Travel, 5),
        new(Workforce.Victor, [Escalate], [DayOfWeek.Friday], [TimeOfDay.Evening], ClaimType.Expense, 3),

        // The assistant approves the small claims on Dana's behalf. Same habit, different initiator.
        new(Workforce.Assistant, [Approve], _weekdays, TimeOfDay.WorkingDay, ClaimType.Expense, 3),

        // Sam is the control: any day, any time, any decision. Nothing here should clear the threshold.
        new(Workforce.Sam, [Approve, Reject, Escalate], _everyDay, TimeOfDay.WorkingDay, ClaimType.Expense, 4)
    ];

    /// <summary>
    /// Appends the history, skipping anything already in the store.
    /// </summary>
    /// <param name="appender">The <see cref="ActivityAppender"/> to append through.</param>
    /// <param name="weeks">How many weeks of history to generate.</param>
    /// <param name="seed">The seed making a run reproducible.</param>
    /// <param name="onProgress">Called with the running event count as the history is appended.</param>
    /// <returns>A <see cref="SampleHistoryResult"/> describing what was appended.</returns>
    public static async Task<SampleHistoryResult> Generate(
        ActivityAppender appender,
        int weeks,
        int seed,
        Action<int> onProgress)
    {
        var random = new Random(seed);
        var reports = 0;
        var events = 0;
        var skipped = 0;
        var index = 0;

        var firstMonday = MondayOnOrBefore(DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-7 * weeks));

        for (var week = 0; week < weeks; week++)
        {
            foreach (var habit in _habits)
            {
                foreach (var day in habit.Days)
                {
                    var date = firstMonday.AddDays((week * 7) + DaysFromMonday(day));

                    for (var occurrence = 0; occurrence < habit.PerDay; occurrence++)
                    {
                        var appended = await AppendClaim(appender, habit, date, index++, random);

                        if (appended == 0)
                        {
                            skipped++;
                            continue;
                        }

                        reports++;
                        events += appended;
                        onProgress(events);
                    }
                }
            }
        }

        return new SampleHistoryResult(reports, events, skipped);
    }

    /// <summary>
    /// Appends one claim's history - its submission, the decision on it, and any reimbursement that followed.
    /// </summary>
    /// <param name="appender">The <see cref="ActivityAppender"/> to append through.</param>
    /// <param name="habit">The <see cref="Habit"/> being acted out.</param>
    /// <param name="date">The date the decision falls on.</param>
    /// <param name="index">The claim's position in the run, which determines its id.</param>
    /// <param name="random">The seeded <see cref="Random"/> the run varies through.</param>
    /// <returns>The number of events appended, or zero when the claim already existed.</returns>
    static async Task<int> AppendClaim(ActivityAppender appender, Habit habit, DateOnly date, int index, Random random)
    {
        var id = ReportId(index);
        var decidedAt = At(date, habit.Times[random.Next(habit.Times.Length)], random);
        var submitter = Workforce.Submitters[random.Next(Workforce.Submitters.Length)];
        var submittedAt = At(date.AddDays(-random.Next(1, 5)), TimeOfDay.WorkingDay[random.Next(TimeOfDay.WorkingDay.Length)], random);
        var amount = new Money(Math.Round((decimal)(random.NextDouble() * 4000) + 20, 2));
        var category = Workforce.Categories[random.Next(Workforce.Categories.Length)];

        var submission = await appender.Append(
            id,
            new ExpenseReportSubmitted(submitter.Name, amount, category),
            submitter,
            submittedAt,
            habit.ClaimType,
            Submit);

        // The submission is guarded by a uniqueness constraint, so a claim generated on an earlier run is
        // rejected here and the rest of its history is skipped rather than duplicated.
        if (!submission.IsSuccess)
        {
            return 0;
        }

        var command = habit.Commands[random.Next(habit.Commands.Length)];
        var reason = Workforce.Reasons[random.Next(Workforce.Reasons.Length)];

        object decision = command switch
        {
            Approve => new ExpenseReportApproved(habit.Actor.Name),
            Reject => new ExpenseReportRejected(habit.Actor.Name, reason),
            _ => new ExpenseReportEscalated(habit.Actor.Name, reason)
        };

        await appender.Append(id, decision, habit.Actor, decidedAt, habit.ClaimType, command, causedByCommand: Submit);

        if (command != Approve)
        {
            return 2;
        }

        // Approved claims are paid out by the nightly run on the following Tuesday - a habit of the system
        // itself, and the one pattern in the set whose initiator is neither a person nor an agent.
        var payoutDate = NextTuesday(DateOnly.FromDateTime(decidedAt.Date));
        var paidAt = new DateTimeOffset(payoutDate.Year, payoutDate.Month, payoutDate.Day, 2, random.Next(0, 60), random.Next(0, 60), TimeSpan.Zero);
        var system = new Actor("Reimbursement run", Identity.System);

        await appender.Append(id, new ExpenseReportReimbursed(amount), system, paidAt, habit.ClaimType, Reimburse, causedByCommand: Approve);

        return 3;
    }

    /// <summary>
    /// Builds a report id from its position in the run, so a re-run produces the same ids and collides with what
    /// is already stored instead of doubling the history.
    /// </summary>
    /// <param name="index">The claim's position in the run.</param>
    /// <returns>The <see cref="ExpenseReportId"/> for that position.</returns>
    static ExpenseReportId ReportId(int index)
    {
        var bytes = new byte[16];
        BitConverter.TryWriteBytes(bytes, index);
        bytes[15] = 0xE5;
        return new Guid(bytes);
    }

    static DateTimeOffset At(DateOnly date, TimeOfDay time, Random random) =>
        new(date.Year, date.Month, date.Day, random.Next(time.FirstHour, time.LastHour + 1), random.Next(0, 60), random.Next(0, 60), TimeSpan.Zero);

    static DateOnly MondayOnOrBefore(DateOnly date) => date.AddDays(-DaysFromMonday(date.DayOfWeek));

    static DateOnly NextTuesday(DateOnly date) => date.AddDays(((DaysFromMonday(DayOfWeek.Tuesday) - DaysFromMonday(date.DayOfWeek) + 7 - 1) % 7) + 1);

    static int DaysFromMonday(DayOfWeek day) => ((int)day + 6) % 7;

    /// <summary>
    /// A recurring behavior to bake into the history.
    /// </summary>
    /// <param name="Actor">Who acts.</param>
    /// <param name="Commands">The commands they carry out - more than one means no single command is habitual.</param>
    /// <param name="Days">The days they act on.</param>
    /// <param name="Times">The parts of the day they act in - more than one means no time is habitual.</param>
    /// <param name="ClaimType">The kind of claim they act on.</param>
    /// <param name="PerDay">How many claims they get through on one of those days.</param>
    sealed record Habit(
        Actor Actor,
        string[] Commands,
        DayOfWeek[] Days,
        TimeOfDay[] Times,
        ClaimType ClaimType,
        int PerDay);
}
