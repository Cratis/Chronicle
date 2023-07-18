// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Connections;

/// <summary>
/// Exception that gets thrown when a connection timed out.
/// </summary>
public class ConnectionTimedOut : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionTimedOut"/> class.
    /// </summary>
    public ConnectionTimedOut() : base("Connection timed out when trying to connect to the Cratis Kernel")
    {
    }
}
