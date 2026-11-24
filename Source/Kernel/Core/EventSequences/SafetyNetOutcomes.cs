// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// The values of what the safety net found when it looked at a dormant observer, as the check counter reports them (decision 0034).
/// </summary>
internal static class SafetyNetOutcomes
{
    /// <summary>No event the observer observes is waiting for it; it was not activated.</summary>
    internal const string UpToDate = "up_to_date";

    /// <summary>An event the observer observes is waiting for it; it was woken.</summary>
    internal const string Behind = "behind";

    /// <summary>The cursor or the tail could not be read, so the observer was woken.</summary>
    internal const string Unverified = "unverified";

    /// <summary>The observer went the full interval without a wake and was woken whatever the comparison said.</summary>
    internal const string FullWake = "full_wake";
}
