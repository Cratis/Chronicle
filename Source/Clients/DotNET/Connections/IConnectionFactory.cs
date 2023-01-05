// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Connections;

/// <summary>
/// Defines a factory for <see cref="IConnection"/>.
/// </summary>
public interface IConnectionFactory
{
    /// <summary>
    /// Gets a connection.
    /// </summary>
    /// <returns>The <see cref="IConnection"/> instance.</returns>
    Task<IConnection> GetConnection();
}
