// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Alerts;

/// <summary>
/// Represents an incident transition kind.
/// </summary>
public enum AlertIncidentTransitionKind
{
    /// <summary>
    /// Opens or reopens an incident.
    /// </summary>
    Raised = 0,

    /// <summary>
    /// Changes an open incident.
    /// </summary>
    Escalated = 1,

    /// <summary>
    /// Closes an incident, retaining a tombstone.
    /// </summary>
    Cleared = 2
}
