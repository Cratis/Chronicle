// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Identities;

namespace Samples.Backoffice;

/// <summary>
/// Represents somebody - or something - that does work in the back office.
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
    /// and it is also what files the behavior under the person rather than under the agent.
    /// </remarks>
    public static Actor Agent(string name, string userName, Actor onBehalfOf) =>
        new(name, new Identity(userName, name, userName, onBehalfOf.Identity));
}

/// <summary>
/// The part of the day something happens in.
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
    /// <summary>Between five and eight.</summary>
    public static readonly TimeOfDay EarlyMorning = new("EarlyMorning", 6, 7);

    /// <summary>Between eight and eleven.</summary>
    public static readonly TimeOfDay Morning = new("Morning", 8, 10);

    /// <summary>Between eleven and two.</summary>
    public static readonly TimeOfDay Midday = new("Midday", 11, 13);

    /// <summary>Between two and five.</summary>
    public static readonly TimeOfDay Afternoon = new("Afternoon", 14, 16);

    /// <summary>Between five and ten.</summary>
    public static readonly TimeOfDay Evening = new("Evening", 17, 20);

    /// <summary>Between ten and five.</summary>
    public static readonly TimeOfDay Night = new("Night", 22, 23);

    /// <summary>Every bucket, for actors with no time-of-day habit.</summary>
    public static readonly TimeOfDay[] AnyTime = [EarlyMorning, Morning, Midday, Afternoon, Evening, Night];
}

/// <summary>
/// The people who work here and the vocabulary they work with.
/// </summary>
/// <remarks>
/// Everybody here is a colleague, and everybody does several different jobs. There are no outsiders who only ever
/// do one thing - that shape is what made an earlier version of this sample uninteresting, because a person with
/// one task has a heatmap with one block in it and a pattern set worth nothing to browse.
/// </remarks>
public static class Workforce
{
    /// <summary>Accounts payable. Enters invoices, matches them, and chases the ones that do not add up.</summary>
    public static readonly Actor Ingrid = Actor.Person("Ingrid Holm", "ingrid.holm");

    /// <summary>Financial controller. Posts to the ledger, releases payments, closes the period.</summary>
    public static readonly Actor Petter = Actor.Person("Petter Aas", "petter.aas");

    /// <summary>HR advisor. Handles leave, screens applicants, gets new starters going.</summary>
    public static readonly Actor Rania = Actor.Person("Rania Haddad", "rania.haddad");

    /// <summary>Payroll. Checks hours, runs the pay, and fields the questions that follow.</summary>
    public static readonly Actor Jonas = Actor.Person("Jonas Vik", "jonas.vik");

    /// <summary>Procurement. Raises orders, accepts quotes, reviews how suppliers are doing.</summary>
    public static readonly Actor Mira = Actor.Person("Mira Sandhu", "mira.sandhu");

    /// <summary>Office manager. Covers whatever is short-handed, whenever - the one person with no routine.</summary>
    public static readonly Actor Alex = Actor.Person("Alex Berg", "alex.berg");

    /// <summary>Reads the invoice mailbox overnight and enters what it can, on Ingrid's behalf.</summary>
    public static readonly Actor InvoiceAgent = Actor.Agent("Invoice capture", "agent.invoices", Ingrid);

    /// <summary>Checks the straightforward timesheets overnight, on Jonas's behalf.</summary>
    public static readonly Actor TimesheetAgent = Actor.Agent("Timesheet checker", "agent.timesheets", Jonas);

    /// <summary>The overnight run, which belongs to nobody.</summary>
    public static readonly Actor Overnight = new("Overnight run", Identity.System);

    /// <summary>
    /// Everybody who works here, for the things everybody does - handing in hours, asking for time off.
    /// </summary>
    /// <remarks>
    /// These give every scope a light background of ordinary employee activity underneath its specialist work,
    /// which is what a real person's history looks like and what keeps a heatmap from being one solid block.
    /// </remarks>
    public static readonly Actor[] Everybody = [Ingrid, Petter, Rania, Jonas, Mira, Alex];

    /// <summary>The companies we buy from.</summary>
    public static readonly SupplierName[] Suppliers =
    [
        "Nordic Paper",
        "Halvorsen Elektro",
        "Lindgren Kontor",
        "Vestland Frakt",
        "Sørby IT",
        "Aker Rengjøring"
    ];

    /// <summary>The accounts things get posted to.</summary>
    public static readonly Reference[] Accounts = ["4010", "5200", "6300", "6540", "7140", "7790"];

    /// <summary>The positions being recruited for.</summary>
    public static readonly Reference[] Positions =
    [
        "Warehouse coordinator",
        "Financial analyst",
        "Service technician",
        "Customer adviser"
    ];

    /// <summary>Why something gets disputed, declined or turned down.</summary>
    public static readonly Reason[] Reasons =
    [
        "No matching purchase order",
        "Quantity does not agree",
        "Priced above the agreed rate",
        "Clashes with another booking",
        "Not enough notice given",
        "Experience does not match the role"
    ];
}
