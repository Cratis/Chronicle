// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.Events;

/// <summary>
/// Converts between <see cref="AppendedEvent"/> and its contract representation.
/// </summary>
internal static class AppendedEventConverters
{
    /// <summary>
    /// Converts a contract <see cref="Contracts.Events.AppendedEvent"/> to an <see cref="AppendedEvent"/>.
    /// </summary>
    /// <param name="appendedEvent">The contract appended event to convert.</param>
    /// <returns>The converted appended event.</returns>
    public static AppendedEvent ToApi(this Contracts.Events.AppendedEvent appendedEvent) => new(
        appendedEvent.Context.ToApi(),
        appendedEvent.Content);

    /// <summary>
    /// Converts a collection of contract <see cref="Contracts.Events.AppendedEvent"/> to a collection of <see cref="AppendedEvent"/>.
    /// </summary>
    /// <param name="appendedEvents">The collection of contract appended events to convert.</param>
    /// <returns>The converted collection of appended events.</returns>
    public static IEnumerable<AppendedEvent> ToApi(this IEnumerable<Contracts.Events.AppendedEvent> appendedEvents) =>
        appendedEvents.Select(e => e.ToApi()).ToArray();

    /// <summary>
    /// Converts an <see cref="AppendedEvent"/> to a contract <see cref="Contracts.Events.AppendedEvent"/>.
    /// </summary>
    /// <param name="appendedEvent">The appended event to convert.</param>
    /// <returns>The converted contract appended event.</returns>
    public static Contracts.Events.AppendedEvent ToContract(this AppendedEvent appendedEvent) => new()
    {
        Context = appendedEvent.Context.ToContract(),
        Content = appendedEvent.Content
    };
}
