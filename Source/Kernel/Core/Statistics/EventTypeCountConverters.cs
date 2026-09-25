// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Statistics;

/// <summary>
/// Converter methods for <see cref="EventTypeCount"/>.
/// </summary>
public static class EventTypeCountConverters
{
    /// <summary>
    /// Converts an <see cref="EventTypeCount"/> to its generated contract representation.
    /// </summary>
    /// <param name="count">The <see cref="EventTypeCount"/> to convert.</param>
    /// <returns>The converted contract.</returns>
    public static Contracts.Statistics.EventTypeCount ToContract(this EventTypeCount count) =>
        new()
        {
            EventType = count.EventType,
            Count = count.Count
        };
}
