// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Exception that gets thrown when a connection timed out.
/// </summary>
public class ConnectionTimedOut : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionTimedOut"/> class.
    /// </summary>
    public ConnectionTimedOut() : base("Connection timed out when trying to connect to the Chronicle")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionTimedOut"/> class with the connection attempt's candidates.
    /// </summary>
    /// <param name="candidates">The redacted candidate server addresses.</param>
    /// <param name="secondsToWait">The connection timeout in seconds.</param>
    public ConnectionTimedOut(string candidates, int secondsToWait)
        : base($"Connection timed out after {secondsToWait} seconds when trying to connect to Chronicle servers: {candidates}")
    {
    }
}
