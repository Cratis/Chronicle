// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Statistics;

/// <summary>
/// Represents how many events of one event type an event store holds in one namespace.
/// </summary>
/// <remarks>
/// <para>
/// One row per (event type, namespace) pair. Every operational figure the dashboard and the event store cards
/// show is a fold over these rows - total events is their sum, the event type count is how many distinct event
/// types appear, the namespace count is how many distinct namespaces do.
/// </para>
/// <para>
/// This is deliberately the narrowest shape that answers all of them. A read model per figure would be several
/// observers over the same events computing sums of each other; a row per pair is the one grain of information
/// none of the figures can be derived without.
/// </para>
/// </remarks>
public class EventTypeStatistics
{
    /// <summary>
    /// Gets or sets the identifier of the event type the row counts.
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the namespace the events were appended in.
    /// </summary>
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets how many events of the type the namespace holds.
    /// </summary>
    public int Count { get; set; }
}
