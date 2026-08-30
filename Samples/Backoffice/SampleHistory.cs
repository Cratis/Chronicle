// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using System.Linq;

namespace Samples.Backoffice;

/// <summary>
/// Describes what a generation run produced.
/// </summary>
/// <param name="Events">The number of events appended.</param>
/// <param name="AlreadyGenerated">Whether the store already held the history and was left alone.</param>
public record SampleHistoryResult(int Events, bool AlreadyGenerated);

/// <summary>
/// Generates a backdated history of back-office work with recurring behavior deliberately baked into it.
/// </summary>
/// <remarks>
/// The thing worth getting right is not volume, it is that <b>nobody does only one thing</b>. Everybody here holds
/// three or four different jobs at different points in the week, plus the ordinary employee work everybody does, so
/// a person's heatmap lights up in several places with several commands rather than showing one block. An earlier
/// version of this sample gave each person a single task, and the result was a set of scopes that all looked alike
/// and a heatmap with one operation dominating it - which is exactly what a pattern browser must not demonstrate.
/// <para>
/// Alex is the counterweight: he covers whatever is short-handed, at no particular time, and should establish
/// nothing anybody would call a routine.
/// </para>
/// <para>
/// Every event is appended with an explicit <c>occurred</c>, because the whole history happened before the store
/// was ever run, and the whole thing is planned first and appended in the order things happened. A real log is the
/// week's work interleaved as people do it, and the miner reads the stream once in order.
/// </para>
/// </remarks>
public static class SampleHistory
{
    static readonly DayOfWeek[] _weekdays = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday];
    static readonly DayOfWeek[] _everyDay = [.. Enum.GetValues<DayOfWeek>()];

    static readonly Habit[] _habits =
    [

        // Ingrid works accounts payable: the post first thing, matching over lunch, and the awkward ones on a
        // Thursday when she has time to phone suppliers.
        new(Workforce.Ingrid, Activity.InvoiceRegistration, _weekdays, [TimeOfDay.EarlyMorning], 7),
        new(Workforce.Ingrid, Activity.InvoiceMatching, _weekdays, [TimeOfDay.Midday], 4),
        new(Workforce.Ingrid, Activity.InvoiceDispute, [DayOfWeek.Thursday], [TimeOfDay.Afternoon], 3),

        // Petter controls the finances: postings through the afternoon, payments released twice a week, and the
        // period closed on a Friday evening once everything else has settled.
        new(Workforce.Petter, Activity.LedgerPosting, _weekdays, [TimeOfDay.Afternoon], 6),
        new(Workforce.Petter, Activity.PaymentApproval, [DayOfWeek.Tuesday, DayOfWeek.Friday], [TimeOfDay.Midday], 5),
        new(Workforce.Petter, Activity.PeriodClose, [DayOfWeek.Friday], [TimeOfDay.Evening], 2),

        // Rania runs HR: leave decided before the day starts, applications read midweek, new starters on a Monday.
        new(Workforce.Rania, Activity.LeaveDecision, _weekdays, [TimeOfDay.EarlyMorning], 5),
        new(Workforce.Rania, Activity.CandidateScreening, [DayOfWeek.Tuesday, DayOfWeek.Wednesday], [TimeOfDay.Afternoon], 4),
        new(Workforce.Rania, Activity.PayrollQuery, [DayOfWeek.Monday], [TimeOfDay.Morning], 3),

        // Jonas runs payroll: hours checked at the start of the week, the pay run on a Thursday evening, and the
        // questions that follow it through the afternoons.
        new(Workforce.Jonas, Activity.TimesheetReview, [DayOfWeek.Monday], [TimeOfDay.Morning], 6),
        new(Workforce.Jonas, Activity.PayrollProcessing, [DayOfWeek.Thursday], [TimeOfDay.Evening], 3),
        new(Workforce.Jonas, Activity.PayrollQuery, _weekdays, [TimeOfDay.Afternoon], 4),

        // Mira buys: orders over lunch, quotes accepted on a Wednesday morning, suppliers reviewed on a Friday.
        new(Workforce.Mira, Activity.PurchaseOrdering, _weekdays, [TimeOfDay.Midday], 5),
        new(Workforce.Mira, Activity.QuoteApproval, [DayOfWeek.Wednesday], [TimeOfDay.Morning], 3),
        new(Workforce.Mira, Activity.SupplierReview, [DayOfWeek.Friday], [TimeOfDay.Afternoon], 3),

        // The agents work overnight on somebody's behalf, so their work files under the person.
        new(Workforce.InvoiceAgent, Activity.InvoiceRegistration, _weekdays, [TimeOfDay.Night], 5),
        new(Workforce.TimesheetAgent, Activity.TimesheetReview, _weekdays, [TimeOfDay.Night], 4),

        // The overnight run belongs to nobody.
        new(Workforce.Overnight, Activity.LedgerReconciliation, _weekdays, [TimeOfDay.Night], 2),
        new(Workforce.Overnight, Activity.PeriodArchive, [DayOfWeek.Sunday], [TimeOfDay.Night], 1),

        // What everybody does on top of their job - hours in at the end of the week, leave asked for now and then.
        new(Workforce.Ingrid, Activity.OwnTimesheet, [DayOfWeek.Friday], [TimeOfDay.Evening], 1),
        new(Workforce.Petter, Activity.OwnTimesheet, [DayOfWeek.Friday], [TimeOfDay.Evening], 1),
        new(Workforce.Rania, Activity.OwnTimesheet, [DayOfWeek.Friday], [TimeOfDay.Evening], 1),
        new(Workforce.Jonas, Activity.OwnTimesheet, [DayOfWeek.Friday], [TimeOfDay.Evening], 1),
        new(Workforce.Mira, Activity.OwnTimesheet, [DayOfWeek.Friday], [TimeOfDay.Evening], 1),

        // Alex is the control: whatever needs covering, whenever.
        new(Workforce.Alex, Activity.InvoiceRegistration, _everyDay, TimeOfDay.AnyTime, 1),
        new(Workforce.Alex, Activity.LeaveDecision, _everyDay, TimeOfDay.AnyTime, 1),
        new(Workforce.Alex, Activity.PurchaseOrdering, _everyDay, TimeOfDay.AnyTime, 1),
        new(Workforce.Alex, Activity.PayrollQuery, _everyDay, TimeOfDay.AnyTime, 1),
        new(Workforce.Alex, Activity.CandidateScreening, _everyDay, TimeOfDay.AnyTime, 1)
    ];

    /// <summary>
    /// Appends the history, unless the store already holds it.
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
        // The marker goes down first and is guarded by a uniqueness constraint, so a second run is turned away
        // here rather than laying a duplicate history on top of the first.
        var marker = await appender.Append(
            EventSourceId.New(),
            new SampleHistoryGenerated(new Quantity(seed)),
            Workforce.Overnight,
            DateTimeOffset.UtcNow,
            AggregateType.SampleData,
            Commands.GenerateSampleHistory);

        if (!marker.IsSuccess)
        {
            return new SampleHistoryResult(0, true);
        }

        var random = new Random(seed);
        var planned = new List<PlannedEvent>();
        var index = 0;

        var firstMonday = MondayOnOrBefore(DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-7 * weeks));

        for (var week = 0; week < weeks; week++)
        {
            foreach (var habit in _habits)
            {
                foreach (var date in habit.Days.Select(day => firstMonday.AddDays((week * 7) + DaysFromMonday(day))))
                {
                    for (var occurrence = 0; occurrence < habit.PerDay; occurrence++)
                    {
                        var at = At(date, habit.Times[random.Next(habit.Times.Length)], random);
                        planned.AddRange(ActivityWriter.Plan(habit.Activity, habit.Actor, at, index++, random));
                    }
                }
            }
        }

        // Planned a habit at a time, appended in the order things happened. A real log is the week's work
        // interleaved as people do it, not half a year of one person followed by half a year of the next, and the
        // miner reads the stream once in order - so the order it arrives in is part of what it learns.
        var events = 1;
        foreach (var plan in planned.OrderBy(_ => _.Occurred))
        {
            await appender.Append(plan.EventSourceId, plan.Event, plan.Actor, plan.Occurred, plan.Aggregate, plan.CommandType, plan.CausedByCommand);
            events++;
            onProgress(events);
        }

        return new SampleHistoryResult(events, false);
    }

    /// <summary>
    /// A moment within a part of the day.
    /// </summary>
    /// <param name="date">The day it falls on.</param>
    /// <param name="time">The part of the day it falls in.</param>
    /// <param name="random">The seeded <see cref="Random"/> the run varies through.</param>
    /// <returns>The moment.</returns>
    /// <remarks>
    /// Minutes stop short of the hour so the few minutes a follow-up event lands later stay inside the same bucket,
    /// rather than smearing one habit across two of them.
    /// </remarks>
    static DateTimeOffset At(DateOnly date, TimeOfDay time, Random random) =>
        new(date.Year, date.Month, date.Day, random.Next(time.FirstHour, time.LastHour + 1), random.Next(0, 50), random.Next(0, 60), TimeSpan.Zero);

    static DateOnly MondayOnOrBefore(DateOnly date) => date.AddDays(-DaysFromMonday(date.DayOfWeek));

    static int DaysFromMonday(DayOfWeek day) => ((int)day + 6) % 7;

    /// <summary>
    /// A recurring piece of work to bake into the history.
    /// </summary>
    /// <param name="Actor">Who does it.</param>
    /// <param name="Activity">What they do.</param>
    /// <param name="Days">The days they do it on.</param>
    /// <param name="Times">The parts of the day they do it in - more than one means no time is habitual.</param>
    /// <param name="PerDay">How many they get through on one of those days.</param>
    sealed record Habit(
        Actor Actor,
        Activity Activity,
        DayOfWeek[] Days,
        TimeOfDay[] Times,
        int PerDay);
}
