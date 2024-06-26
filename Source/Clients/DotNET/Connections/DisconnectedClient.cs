// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Exception that gets thrown when the Cratis client is not connected and one is attempting to perform an operation.
/// </summary>
public class DisconnectedClient : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DisconnectedClient"/> class.
    /// </summary>
    public DisconnectedClient() : base("The Cratis client is disconnected from the Kernel.")
    {
    }
}
