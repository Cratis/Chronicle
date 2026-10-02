// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Alerts;

/// <summary>
/// Represents an incident write outcome.
/// </summary>
public enum AlertIncidentWriteOutcome
{
    /// <summary>
    /// The transition was applied.
    /// </summary>
    Applied = 0,

    /// <summary>
    /// An equal or newer transition already exists.
    /// </summary>
    AlreadyApplied = 1,

    /// <summary>
    /// No open incident exists to escalate.
    /// </summary>
    OrphanEscalation = 2
}
