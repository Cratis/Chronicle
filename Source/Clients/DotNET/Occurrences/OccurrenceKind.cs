// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// Identifies the source of an occurrence, outside the domain event log.
/// </summary>
public enum OccurrenceKind
{
    /// <summary>
    /// Recorded by an application.
    /// </summary>
    Application = 0,

    /// <summary>
    /// Materialized from a UTC schedule.
    /// </summary>
    Schedule = 1,

    /// <summary>
    /// Starts a logical activation.
    /// </summary>
    Startup = 2,

    /// <summary>
    /// Ends a logical activation.
    /// </summary>
    Shutdown = 3
}
