// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Samples.Storefront;

/// <summary>
/// An event the generator intends to append, before it knows where in the history it belongs.
/// </summary>
/// <param name="EventSourceId">The <see cref="Cratis.Chronicle.Events.EventSourceId"/> to append for.</param>
/// <param name="Event">The event.</param>
/// <param name="Actor">Who is carrying out the command.</param>
/// <param name="Occurred">When it happened.</param>
/// <param name="Aggregate">What kind of thing it happened to.</param>
/// <param name="CommandType">The command being carried out.</param>
/// <param name="CausedByCommand">The command that led to this one, if any.</param>
/// <remarks>
/// The generator works a habit at a time, so it produces the history badly out of order - a whole half-year of one
/// person's mornings, then a half-year of somebody else's. Planning the events first and appending them in the
/// order they happened is what makes the stream look like a real store's, where the day's work arrives interleaved
/// as people do it.
/// <para>
/// This is not cosmetic. The miner reads a stream once, in order, and keeps only what stays frequent as it goes -
/// so a person whose entire history arrives in one uninterrupted block, long after everybody else's, is counted
/// very differently from one whose work is spread through the stream the way it actually happened.
/// </para>
/// </remarks>
public record PlannedEvent(
    EventSourceId EventSourceId,
    object Event,
    Actor Actor,
    DateTimeOffset Occurred,
    AggregateType Aggregate,
    string CommandType,
    string? CausedByCommand = default);
