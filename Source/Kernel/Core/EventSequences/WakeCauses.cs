// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// The values of what woke a dormant observer, as the wake counter reports them (decision 0034).
/// </summary>
internal static class WakeCauses
{
    /// <summary>An event the observer observes was appended.</summary>
    internal const string Event = "event";

    /// <summary>The safety net of the queue woke the observer to find out what its record cannot say.</summary>
    internal const string SafetyNet = "safety_net";
}
