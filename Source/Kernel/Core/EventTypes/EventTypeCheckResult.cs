// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventTypes;

/// <summary>
/// Represents what checking one event type registration found.
/// </summary>
/// <param name="EventType">The event type identifier.</param>
/// <param name="Generation">The generation the finding is about.</param>
/// <param name="Accepted">Whether registering would be accepted.</param>
/// <param name="Reason">Why registering would be rejected, or a warning when an accepted registration adds protection; otherwise empty.</param>
/// <param name="Keyword">The incompatible schema keyword, or protection when an accepted registration adds protection to an existing property; otherwise empty.</param>
/// <param name="Path">The JSON path of the incompatible declaration or existing property gaining protection; otherwise empty.</param>
public record EventTypeCheckResult(string EventType, uint Generation, bool Accepted, string Reason, string Keyword, string Path)
{
    /// <summary>
    /// Gets the schema relationship (Identical, Tolerated, Additive, Subset or Breaking) independently of whether the server policy accepts it.
    /// </summary>
    public string Classification { get; init; } = string.Empty;

    /// <summary>
    /// Creates the result of a registration that would be accepted.
    /// </summary>
    /// <param name="eventType">The event type identifier.</param>
    /// <param name="generation">The generation checked.</param>
    /// <returns>The <see cref="EventTypeCheckResult"/>.</returns>
    public static EventTypeCheckResult Accept(string eventType, uint generation) => new(eventType, generation, true, string.Empty, string.Empty, string.Empty);

    /// <summary>
    /// Creates the result of a registration that would be rejected.
    /// </summary>
    /// <param name="eventType">The event type identifier.</param>
    /// <param name="generation">The generation the rejection is about.</param>
    /// <param name="reason">Why it would be rejected.</param>
    /// <param name="keyword">The schema keyword that differs, if any.</param>
    /// <param name="path">The JSON path of the difference, if any.</param>
    /// <returns>The <see cref="EventTypeCheckResult"/>.</returns>
    public static EventTypeCheckResult Reject(string eventType, uint generation, string reason, string keyword = "", string path = "") =>
        new(eventType, generation, false, reason, keyword, path);
}
