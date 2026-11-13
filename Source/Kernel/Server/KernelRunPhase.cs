// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server;

/// <summary>
/// Represents the phase a run of the kernel host ended in.
/// </summary>
internal enum KernelRunPhase
{
    /// <summary>
    /// The run ended while the host was starting.
    /// </summary>
    Starting = 0,

    /// <summary>
    /// The run ended while the host was stopping.
    /// </summary>
    Stopping = 1,

    /// <summary>
    /// The run ended while the host's services were being released after the stop.
    /// </summary>
    Disposing = 2,

    /// <summary>
    /// The host started, stopped and released its services.
    /// </summary>
    Ended = 3
}
