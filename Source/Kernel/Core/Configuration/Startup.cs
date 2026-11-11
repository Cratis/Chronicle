// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Configuration for how the server rides out a slow start.
/// </summary>
public class Startup
{
    /// <summary>
    /// Gets how long a required startup step keeps waiting for a silo that is up but not answering before the server
    /// treats it as a real failure and stops.
    /// </summary>
    /// <remarks>
    /// A timeout against this silo or an active sibling usually means it is busy catching up on a backlog, and
    /// restarting only restarts the same backlog, so the step waits without counting attempts. A grain that never
    /// answers - a deadlock, or storage that hangs - looks the same, so the wait is not endless. The limit is
    /// measured per step from its first such timeout. The silo stays running and not ready while it waits.
    /// </remarks>
    public TimeSpan BusySiloWaitLimit { get; init; } = TimeSpan.FromHours(2);
}
