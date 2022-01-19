// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Connections
{
    /// <summary>
    /// Defines the client connection manager.
    /// </summary>
    public interface IConnectionManager
    {
        /// <summary>
        /// Gets the current connection identifier.
        /// </summary>
        ConnectionId CurrentConnectionId { get; }

        /// <summary>
        /// Set the current client connections to be in kernel mode.
        /// </summary>
        void SetKernelMode();
    }
}
