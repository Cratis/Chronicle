// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Concepts.Patterns;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Identities;

namespace Samples.Backoffice;

/// <summary>
/// Represents what kind of thing an event source is.
/// </summary>
/// <param name="Name">The name of the kind.</param>
/// <remarks>
/// This becomes the aggregate a mined pattern is about, which is what lets "Ingrid enters invoices first thing"
/// be told apart from "Ingrid matches them at midday" - same person, same week, different work.
/// </remarks>
public record AggregateType(string Name)
{
    /// <summary>A supplier invoice.</summary>
    public static readonly AggregateType Invoice = new("Invoice");

    /// <summary>A commitment to buy something.</summary>
    public static readonly AggregateType PurchaseOrder = new("PurchaseOrder");

    /// <summary>A company we buy from.</summary>
    public static readonly AggregateType Supplier = new("Supplier");

    /// <summary>An accounting period.</summary>
    public static readonly AggregateType Ledger = new("Ledger");

    /// <summary>A request for time off.</summary>
    public static readonly AggregateType LeaveRequest = new("LeaveRequest");

    /// <summary>Somebody applying for a job.</summary>
    public static readonly AggregateType Candidate = new("Candidate");

    /// <summary>Somebody's hours for a period.</summary>
    public static readonly AggregateType Timesheet = new("Timesheet");

    /// <summary>The generator's own bookkeeping.</summary>
    public static readonly AggregateType SampleData = new("SampleData");

    /// <summary>
    /// Gets the <see cref="Cratis.Chronicle.Events.EventSourceType"/> events of this kind are appended under.
    /// </summary>
    public EventSourceType EventSourceType => new(Name);
}

/// <summary>
/// Appends an event the way a command would have.
/// </summary>
/// <param name="store">The <see cref="IEventStore"/> to append to.</param>
/// <param name="identityProvider">The <see cref="IIdentityProvider"/> to act as somebody through.</param>
/// <param name="causationManager">The <see cref="ICausationManager"/> to name the acting command through.</param>
/// <remarks>
/// Pattern detection mines the context an event was appended in rather than its content, so a generator that only
/// appended events would produce a store with nothing to mine. Everything the miner reads is set here: who acted,
/// whether they acted for somebody else, which command they were carrying out, what caused that command, what kind
/// of thing was acted on, and - crucially for a backdated history - when it actually happened.
/// <para>
/// An application on Arc gets all of this for free: its command pipeline names the command and its identity
/// provider carries the user. This is what that looks like when appending through the client directly.
/// </para>
/// </remarks>
public class ActivityAppender(IEventStore store, IIdentityProvider identityProvider, ICausationManager causationManager)
{
    /// <summary>
    /// The causation type used for a link representing a command.
    /// </summary>
    /// <remarks>
    /// Arc's command pipeline records this same type. Chronicle does not require the value - it reads the command
    /// name from <see cref="WellKnownCausationProperties.CommandType"/> and only needs the type to be something
    /// other than root or unknown - but matching Arc keeps a store consistent whichever way events reached it.
    /// </remarks>
    public static readonly CausationType Command = new("Command");

    /// <summary>
    /// Appends an event as an actor carrying out a named command.
    /// </summary>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> to append for.</param>
    /// <param name="event">The event to append.</param>
    /// <param name="actor">The <see cref="Actor"/> carrying out the command.</param>
    /// <param name="occurred">When it happened.</param>
    /// <param name="aggregate">The <see cref="AggregateType"/> the event source is.</param>
    /// <param name="commandType">The name of the command being carried out.</param>
    /// <param name="causedByCommand">Optional name of the command that led to this one.</param>
    /// <returns>The <see cref="AppendResult"/>.</returns>
    public async Task<AppendResult> Append(
        EventSourceId eventSourceId,
        object @event,
        Actor actor,
        DateTimeOffset occurred,
        AggregateType aggregate,
        string commandType,
        string? causedByCommand = default)
    {
        identityProvider.SetCurrentIdentity(actor.Identity);

        // The chain reads from the root outwards, so the cause is opened first and the command carrying it out
        // second - which is the order the miner reads back as "this command, caused by that one".
        using var causeScope = causedByCommand is null ? null : causationManager.BeginScope(Command, PropertiesFor(causedByCommand));
        using var commandScope = causationManager.BeginScope(Command, PropertiesFor(commandType));

        return await store.EventLog.Append(
            eventSourceId,
            @event,
            eventSourceType: aggregate.EventSourceType,
            occurred: occurred);
    }

    static Dictionary<string, string> PropertiesFor(string commandType) =>
        new(StringComparer.Ordinal) { [WellKnownCausationProperties.CommandType] = commandType };
}
