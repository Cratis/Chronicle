// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Identities;

namespace Samples.ExpenseApprovals;

/// <summary>
/// Represents somebody - or something - that acts on expense reports.
/// </summary>
/// <param name="Name">The display name.</param>
/// <param name="Identity">The <see cref="Identity"/> events are appended as.</param>
public record Actor(PersonName Name, Identity Identity)
{
    /// <summary>
    /// Creates an actor representing a person acting as themselves.
    /// </summary>
    /// <param name="name">The person's display name.</param>
    /// <param name="userName">The person's user name, used as the subject.</param>
    /// <returns>The <see cref="Actor"/>.</returns>
    public static Actor Person(string name, string userName) =>
        new(name, new Identity(userName, name, userName));

    /// <summary>
    /// Creates an actor representing an agent acting for somebody.
    /// </summary>
    /// <param name="name">The agent's display name.</param>
    /// <param name="userName">The agent's user name, used as the subject.</param>
    /// <param name="onBehalfOf">The person the agent acts for.</param>
    /// <returns>The <see cref="Actor"/>.</returns>
    /// <remarks>
    /// The delegation chain is what makes this an agent rather than a user as far as pattern mining is concerned,
    /// and it is also what files the behavior under the person rather than under the agent - so an assistant
    /// approving on Dana's behalf reinforces Dana's habit instead of establishing a separate one of its own.
    /// </remarks>
    public static Actor Agent(string name, string userName, Actor onBehalfOf) =>
        new(name, new Identity(userName, name, userName, onBehalfOf.Identity));
}

/// <summary>
/// The part of the day a habit happens in.
/// </summary>
/// <param name="Name">The name, matching the bucket Chronicle derives from the hour.</param>
/// <param name="FirstHour">The first hour in the bucket.</param>
/// <param name="LastHour">The last hour in the bucket.</param>
/// <remarks>
/// These mirror Chronicle's own time bucketing so a habit placed in "Morning" lands in the Morning bucket rather
/// than straddling two of them. Keep them in step with the kernel's <c>TimeBucketResolver</c>.
/// </remarks>
public record TimeOfDay(string Name, int FirstHour, int LastHour)
{
    /// <summary>
    /// Between five and eight.
    /// </summary>
    public static readonly TimeOfDay EarlyMorning = new("EarlyMorning", 5, 7);

    /// <summary>
    /// Between eight and eleven.
    /// </summary>
    public static readonly TimeOfDay Morning = new("Morning", 8, 10);

    /// <summary>
    /// Between eleven and two.
    /// </summary>
    public static readonly TimeOfDay Midday = new("Midday", 11, 13);

    /// <summary>
    /// Between two and five.
    /// </summary>
    public static readonly TimeOfDay Afternoon = new("Afternoon", 14, 16);

    /// <summary>
    /// Between five and ten.
    /// </summary>
    public static readonly TimeOfDay Evening = new("Evening", 17, 21);

    /// <summary>
    /// Every bucket of the working day, for actors with no time-of-day habit.
    /// </summary>
    public static readonly TimeOfDay[] WorkingDay = [EarlyMorning, Morning, Midday, Afternoon, Evening];
}

/// <summary>
/// The cast of the sample and the vocabulary they act on.
/// </summary>
public static class Workforce
{
    /// <summary>
    /// Approves expenses first thing on a Monday, reliably enough to be worth predicting.
    /// </summary>
    public static readonly Actor Dana = Actor.Person("Dana Reeves", "dana.reeves");

    /// <summary>
    /// Works through the queue over lunch, every weekday.
    /// </summary>
    public static readonly Actor Nina = Actor.Person("Nina Osei", "nina.osei");

    /// <summary>
    /// Clears out what is left on a Friday evening, and is the one who turns things down.
    /// </summary>
    public static readonly Actor Victor = Actor.Person("Victor Hale", "victor.hale");

    /// <summary>
    /// Acts whenever, with no habit at all - the control that shows the miner does not invent patterns.
    /// </summary>
    public static readonly Actor Sam = Actor.Person("Sam Doyle", "sam.doyle");

    /// <summary>
    /// Approves the small claims on Dana's behalf, moments after they are submitted.
    /// </summary>
    public static readonly Actor Assistant = Actor.Agent("Expense Assistant", "agent.expenses", Dana);

    /// <summary>
    /// The people who submit expense reports.
    /// </summary>
    public static readonly Actor[] Submitters =
    [
        Actor.Person("Priya Raman", "priya.raman"),
        Actor.Person("Tom Alvarez", "tom.alvarez"),
        Actor.Person("Grete Lindqvist", "grete.lindqvist"),
        Actor.Person("Kofi Mensah", "kofi.mensah"),
        Actor.Person("Hana Ito", "hana.ito")
    ];

    /// <summary>
    /// What expenses get claimed for.
    /// </summary>
    public static readonly ExpenseCategory[] Categories =
    [
        "Travel",
        "Accommodation",
        "Meals",
        "Software",
        "Training",
        "Equipment"
    ];

    /// <summary>
    /// Why a report gets turned down or passed on.
    /// </summary>
    public static readonly Reason[] Reasons =
    [
        "Missing receipt",
        "Outside policy",
        "Above approval limit",
        "Duplicate claim",
        "Needs a cost center"
    ];
}
