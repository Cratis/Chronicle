// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients;

/// <summary>
/// Defines the client connections that are open on the local silo.
/// </summary>
internal interface ILocalClientConnections
{
    /// <summary>
    /// Gets the temporary diagnostic identity of the local connection store.
    /// </summary>
    Guid StoreId { get; }

    /// <summary>
    /// Gets the connections that are open, and have kept responding, on the local silo.
    /// </summary>
    /// <param name="at">The point in time to judge liveness at.</param>
    /// <param name="lapse">The longest gap between handled responses a live connection can have.</param>
    /// <returns>The live <see cref="LiveClientConnection"/>s.</returns>
    IEnumerable<LiveClientConnection> GetLive(DateTimeOffset at, TimeSpan lapse);
}
