// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server;

/// <summary>
/// Represents how a run of the kernel host ended.
/// </summary>
internal enum KernelRunOutcome
{
    /// <summary>
    /// The host started, stopped within the host shutdown timeout and released its services without a failure.
    /// </summary>
    Completed = 0,

    /// <summary>
    /// A stop was requested while the host was still starting, and it stopped instead of continuing.
    /// </summary>
    StoppedDuringStartup = 1,

    /// <summary>
    /// The host failed before it finished starting.
    /// </summary>
    StartFailed = 2,

    /// <summary>
    /// The stop took the whole host shutdown timeout, whether or not a service reported a failure.
    /// </summary>
    StopTimedOut = 3,

    /// <summary>
    /// A service failed while the host was stopping.
    /// </summary>
    StopFailed = 4,

    /// <summary>
    /// The host stopped, but releasing its services afterwards failed.
    /// </summary>
    DisposeFailed = 5
}
