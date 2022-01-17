// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;

namespace Cratis.Connections
{
    /// <summary>
    /// Represents an implementation of <see cref="IConnectionManager"/>.
    /// </summary>
    [Singleton]
    public class ConnectionManager : IConnectionManager
    {
        /// <inheritdoc/>
        public ConnectionId CurrentConnectionId { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionManager"/> class.
        /// </summary>
        public ConnectionManager()
        {
            CurrentConnectionId = Guid.NewGuid().ToString();
        }

        /// <inheritdoc/>
        public void SetKernelMode()
        {
            CurrentConnectionId = ConnectionId.Kernel;
        }
    }
}
