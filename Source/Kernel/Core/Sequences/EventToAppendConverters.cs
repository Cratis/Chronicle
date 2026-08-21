// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Converts between <see cref="EventToAppend"/> and its contract representation.
/// </summary>
internal static class EventToAppendConverters
{
    /// <summary>
    /// Converts a contract <see cref="Contracts.Sequences.EventToAppend"/> to an <see cref="EventToAppend"/>.
    /// </summary>
    /// <param name="eventToAppend">The contract event to convert.</param>
    /// <returns>The converted event.</returns>
    public static EventToAppend ToApi(this Contracts.Sequences.EventToAppend eventToAppend) =>
        new(eventToAppend.EventType.ToApi(), eventToAppend.Content, eventToAppend.Subject);
}
