// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Identities;

namespace Samples.Storefront;

/// <summary>
/// Represents somebody - or something - that acts in the storefront.
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
    public static readonly TimeOfDay EarlyMorning = new("EarlyMorning", 5, 7);

    /// <summary>Between eight and eleven.</summary>
    public static readonly TimeOfDay Morning = new("Morning", 8, 10);

    /// <summary>Between eleven and two.</summary>
    public static readonly TimeOfDay Midday = new("Midday", 11, 13);

    /// <summary>Between two and five.</summary>
    public static readonly TimeOfDay Afternoon = new("Afternoon", 14, 16);

    /// <summary>Between five and ten.</summary>
    public static readonly TimeOfDay Evening = new("Evening", 17, 21);

    /// <summary>Between ten and five.</summary>
    public static readonly TimeOfDay Night = new("Night", 22, 23);

    /// <summary>
    /// Every bucket of the working day, for actors with no time-of-day habit.
    /// </summary>
    public static readonly TimeOfDay[] AnyTime = [EarlyMorning, Morning, Midday, Afternoon, Evening, Night];
}

/// <summary>
/// The people who work here, the agents that act for them, and the vocabulary they act on.
/// </summary>
public static class Workforce
{
    /// <summary>Picks and packs the morning's orders, first thing, every weekday.</summary>
    public static readonly Actor Maya = Actor.Person("Maya Chen", "maya.chen");

    /// <summary>Gets the packed shipments out of the door mid-morning.</summary>
    public static readonly Actor Otto = Actor.Person("Otto Brandt", "otto.brandt");

    /// <summary>Works the support queue and the returns that come out of it, afternoons.</summary>
    public static readonly Actor Lena = Actor.Person("Lena Ferrari", "lena.ferrari");

    /// <summary>Places the week's restock orders over lunch on a Monday.</summary>
    public static readonly Actor Ravi = Actor.Person("Ravi Kapoor", "ravi.kapoor");

    /// <summary>Reviews the flagged orders in the evening, when the day's orders are all in.</summary>
    public static readonly Actor Nora = Actor.Person("Nora Sandvik", "nora.sandvik");

    /// <summary>Covers whatever needs covering, whenever - the one person with no routine.</summary>
    public static readonly Actor Tobias = Actor.Person("Tobias Lund", "tobias.lund");

    /// <summary>Adjusts prices overnight on Ravi's behalf.</summary>
    public static readonly Actor PricingAgent = Actor.Agent("Pricing agent", "agent.pricing", Ravi);

    /// <summary>Drafts replies to the easy tickets on Lena's behalf.</summary>
    public static readonly Actor SupportAgent = Actor.Agent("Support assistant", "agent.support", Lena);

    /// <summary>The overnight run, which belongs to nobody.</summary>
    public static readonly Actor Overnight = new("Overnight run", Identity.System);

    /// <summary>
    /// The customers whose orders, returns and tickets give everybody else something to do.
    /// </summary>
    public static readonly Actor[] Customers =
    [
        Actor.Person("Priya Raman", "priya.raman"),
        Actor.Person("Tom Alvarez", "tom.alvarez"),
        Actor.Person("Grete Lindqvist", "grete.lindqvist"),
        Actor.Person("Kofi Mensah", "kofi.mensah"),
        Actor.Person("Hana Ito", "hana.ito"),
        Actor.Person("Ida Solberg", "ida.solberg")
    ];

    /// <summary>The carriers shipments go out with.</summary>
    public static readonly Carrier[] Carriers = ["Nordpost", "Meridian Freight", "CityRunner", "Skylane"];

    /// <summary>What customers open tickets about.</summary>
    public static readonly TicketTopic[] Topics =
    [
        "Where is my order",
        "Wrong item received",
        "Damaged in transit",
        "Change delivery address",
        "Invoice question"
    ];

    /// <summary>Why something gets held, turned down or sent back.</summary>
    public static readonly Reason[] Reasons =
    [
        "Address does not match payment",
        "Unusually large order",
        "Outside the return window",
        "No longer needed",
        "Wrong size",
        "Repeat delivery failure"
    ];
}
