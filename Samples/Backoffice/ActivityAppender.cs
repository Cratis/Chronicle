// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Reflection;
using Cratis.Chronicle;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Concepts.Patterns;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Identities;
using Cratis.Concepts;
using Cratis.Execution;

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
/// <param name="correlationIdModifier">The <see cref="ICorrelationIdModifier"/> to correlate each command through.</param>
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
public class ActivityAppender(
    IEventStore store,
    IIdentityProvider identityProvider,
    ICausationManager causationManager,
    ICorrelationIdModifier correlationIdModifier)
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

        // Every planned event is its own command at its own moment, so each one is correlated separately - which
        // is what an application on Arc gets from its command pipeline, a correlation per command. Leaving them on
        // one correlation would tell Chronicle the whole generated history happened as a single action, collapsing
        // an instance's entire life into one snapshot for anything that reads events back grouped by correlation.
        correlationIdModifier.Modify(CorrelationId.New());

        // The chain reads from the root outwards, so the cause is opened first and the command carrying it out
        // second - which is the order the miner reads back as "this command, caused by that one".
        using var causeScope = causedByCommand is null ? null : causationManager.BeginScope(Command, PropertiesFor(causedByCommand));
        using var commandScope = causationManager.BeginScope(Command, PropertiesFor(commandType, eventSourceId, aggregate, @event));

        return await store.EventLog.Append(
            eventSourceId,
            @event,
            eventSourceType: aggregate.EventSourceType,
            occurred: occurred);
    }

    static Dictionary<string, string> PropertiesFor(string commandType) =>
        new(StringComparer.Ordinal) { [WellKnownCausationProperties.CommandType] = commandType };

    /// <summary>
    /// Builds the causation properties for the command being carried out - its name and the values it was asked to
    /// act on.
    /// </summary>
    /// <param name="commandType">The name of the command.</param>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> the command acted on.</param>
    /// <param name="aggregate">The <see cref="AggregateType"/> the event source is, used to name its id.</param>
    /// <param name="event">The event the command produced, whose values were the command's input.</param>
    /// <returns>The properties to record on the causation.</returns>
    /// <remarks>
    /// Naming the command says which command produced an event but not which invocation of it - two invoices
    /// registered by the same command are indistinguishable on the chain. The values are what separate them, and
    /// they are what an application on Arc records automatically: a command's property values travel on the
    /// causation of every event it appends.
    /// <para>
    /// This sample has no command objects to read those values from - a command here is a name - so it reconstructs
    /// the same set from what the command demonstrably acted on: the event source it targeted, and the values it
    /// put on the event. For <c>RegisterInvoice</c> that is the invoice id plus the supplier, amount and reference,
    /// which is exactly what the real command would have carried.
    /// </para>
    /// <para>
    /// A real application also decides what must <em>not</em> travel this way. Values marked <c>[PII]</c> and
    /// <c>[NotAudited]</c> are withheld, because the causation is written into the event log and stays there for as
    /// long as the events do. Nothing here is sensitive, so nothing is withheld.
    /// </para>
    /// </remarks>
    static Dictionary<string, string> PropertiesFor(
        string commandType,
        EventSourceId eventSourceId,
        AggregateType aggregate,
        object @event)
    {
        var properties = PropertiesFor(commandType);
        properties[$"{CamelCase(aggregate.Name)}Id"] = eventSourceId.Value;

        foreach (var property in @event.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetIndexParameters().Length != 0) continue;
            if (property.GetValue(@event) is not { } value) continue;

            properties[CamelCase(property.Name)] = Render(value);
        }

        return properties;
    }

    /// <summary>
    /// Renders a value the way Arc renders it on a causation.
    /// </summary>
    /// <param name="value">The value to render.</param>
    /// <returns>The rendered value.</returns>
    /// <remarks>
    /// Invariant, and concepts unwrapped to the value they hold. A causation written in one locale has to read the
    /// same in another - a decimal that arrives as "13073,75" on this machine and "13073.75" on the next is not a
    /// value anything downstream can compare.
    /// </remarks>
    static string Render(object value)
    {
        if (value.IsConcept())
        {
            return Render(value.GetConceptValue());
        }

        return value switch
        {
            string text => text,
            DateTimeOffset offset => offset.ToString("O", CultureInfo.InvariantCulture),
            DateTime date => date.ToString("O", CultureInfo.InvariantCulture),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty
        };
    }

    static string CamelCase(string name) =>
        string.IsNullOrEmpty(name) ? name : char.ToLowerInvariant(name[0]) + name[1..];
}
