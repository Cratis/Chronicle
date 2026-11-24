// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Clients;

/// <summary>
/// How busy the server a client is connected to says it is.
/// </summary>
public enum KernelStatus
{
    /// <summary>
    /// The server is not overloaded.
    /// </summary>
    Accepting = 0,

    /// <summary>
    /// The server is overloaded: calls to it may be slow or time out, and the connection is still being served.
    /// </summary>
    Degraded = 1,

    /// <summary>
    /// The server was overloaded and is back under its limits, but has not been for long enough yet to be called accepting.
    /// </summary>
    Recovering = 2
}
