// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Samples.Storefront;

/// <summary>
/// Describes what a generation run produced.
/// </summary>
/// <param name="Events">The number of events appended.</param>
/// <param name="AlreadyGenerated">Whether the store already held the history and was left alone.</param>
public record SampleHistoryResult(int Events, bool AlreadyGenerated);

/// <summary>
/// Generates a backdated history of storefront activity with recurring behavior deliberately baked into it.
/// </summary>
/// <remarks>
/// The point is not volume, it is that different people do different jobs at different times. Maya is in the
/// warehouse first thing; Otto sends the packed shipments out mid-morning; Lena works the support queue and the
/// returns in the afternoon; Ravi places the week's restock over lunch on a Monday; Nora reviews flagged orders in
/// the evening. None of their patterns look like each other's, which is what makes browsing them worth doing.
/// <para>
/// Tobias covers whatever needs covering, at no particular time, and should establish nothing. A miner that reports
/// a habit for him is finding structure in noise, so he is as much a part of the demonstration as the others.
/// </para>
/// <para>
/// Every event is appended with an explicit <c>occurred</c>, because the whole history happened before the store
/// was ever run. Letting the server stamp append time would collapse months of behavior into the few minutes the
/// generator takes, and every mined pattern would be about the afternoon somebody ran this.
/// </para>
/// </remarks>
public static class SampleHistory
{
    static readonly DayOfWeek[] _weekdays = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday];
    static readonly DayOfWeek[] _everyDay = [.. Enum.GetValues<DayOfWeek>()];

    static readonly Habit[] _habits =
    [

        // Maya is in before anyone else, getting the night's orders onto the packing bench.
        new(Workforce.Maya, Activity.Fulfilment, _weekdays, [TimeOfDay.EarlyMorning], 8),

        // Otto sends out what Maya packed, once the carriers start collecting.
        new(Workforce.Otto, Activity.Dispatch, _weekdays, [TimeOfDay.Morning], 6),

        // Lena takes the support queue after lunch, and the returns that come out of it.
        new(Workforce.Lena, Activity.SupportReply, _weekdays, [TimeOfDay.Afternoon], 5),
        new(Workforce.Lena, Activity.ReturnDecision, _weekdays, [TimeOfDay.Afternoon], 3),

        // Ravi places the week's restock over lunch on a Monday.
        new(Workforce.Ravi, Activity.Restocking, [DayOfWeek.Monday], [TimeOfDay.Midday], 6),

        // Nora reviews what got flagged once the day's orders are all in.
        new(Workforce.Nora, Activity.FraudReview, _weekdays, [TimeOfDay.Evening], 4),

        // The assistant drafts replies to the easy tickets overnight, on Lena's behalf.
        new(Workforce.SupportAgent, Activity.SupportReply, _weekdays, [TimeOfDay.Night], 4),

        // Prices move overnight, on Ravi's behalf.
        new(Workforce.PricingAgent, Activity.Repricing, _weekdays, [TimeOfDay.Night], 3),

        // The overnight run tops stock back up before the week starts.
        new(Workforce.Overnight, Activity.Replenishment, [DayOfWeek.Sunday], [TimeOfDay.Night], 2),

        // Tobias is the control: any day, any time, whatever needs doing.
        new(Workforce.Tobias, Activity.SupportReply, _everyDay, TimeOfDay.AnyTime, 1),
        new(Workforce.Tobias, Activity.Fulfilment, _everyDay, TimeOfDay.AnyTime, 1),
        new(Workforce.Tobias, Activity.ReturnDecision, _everyDay, TimeOfDay.AnyTime, 1),
        new(Workforce.Tobias, Activity.Dispatch, _everyDay, TimeOfDay.AnyTime, 1)
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
                foreach (var day in habit.Days)
                {
                    var date = firstMonday.AddDays((week * 7) + DaysFromMonday(day));

                    for (var occurrence = 0; occurrence < habit.PerDay; occurrence++)
                    {
                        var at = At(date, habit.Times[random.Next(habit.Times.Length)], random);
                        planned.AddRange(ActivityWriter.Plan(habit.Activity, habit.Actor, at, index++, random));
                    }
                }
            }
        }

        // Planned a habit at a time, appended in the order things happened. A real store's log is the day's work
        // interleaved as people do it, not a half-year of one person followed by a half-year of the next, and the
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
