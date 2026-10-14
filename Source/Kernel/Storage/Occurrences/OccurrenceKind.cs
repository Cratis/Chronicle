// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Occurrences;

/// <summary>
/// The source of an occurrence.
/// </summary>
public enum OccurrenceKind
{
    /// <summary>
    /// Application.
    /// </summary>
    Application = 0,

    /// <summary>
    /// Schedule.
    /// </summary>
    Schedule = 1,

    /// <summary>
    /// Startup.
    /// </summary>
    Startup = 2,

    /// <summary>
    /// Shutdown.
    /// </summary>
    Shutdown = 3
}
