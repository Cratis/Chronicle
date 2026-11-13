// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventTypes;

/// <summary>
/// Provides extension methods for converting event type check results to their contract representation.
/// </summary>
public static class EventTypeCheckResultConverters
{
    /// <summary>
    /// Converts an <see cref="EventTypeCheckResult"/> to its contract representation.
    /// </summary>
    /// <param name="result">The check result.</param>
    /// <returns>The contract check result.</returns>
    public static Contracts.EventTypes.EventTypeCheckResult ToContract(this EventTypeCheckResult result) =>
        new()
        {
            EventType = result.EventType,
            Generation = result.Generation,
            Accepted = result.Accepted,
            Reason = result.Reason,
            Keyword = result.Keyword,
            Path = result.Path
        };
}
