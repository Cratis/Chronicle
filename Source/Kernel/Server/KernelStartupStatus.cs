// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server;

/// <summary>
/// Represents the startup status of the kernel as seen by readiness and the startup gate.
/// </summary>
internal enum KernelStartupStatus
{
    /// <summary>
    /// The kernel is starting or stopping and does not report standby.
    /// </summary>
    NotReady = 0,

    /// <summary>
    /// The kernel waits for, or has just taken, the single-writer lease in standby.
    /// </summary>
    Standby = 1,

    /// <summary>
    /// The kernel took the lease in standby but did not finish starting within the standby startup timeout.
    /// </summary>
    StandbyExpired = 2,

    /// <summary>
    /// Startup has completed and shutdown has not begun.
    /// </summary>
    Ready = 3
}
