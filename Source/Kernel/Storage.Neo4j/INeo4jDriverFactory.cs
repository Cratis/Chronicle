// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Neo4j.Driver;

namespace Cratis.Chronicle.Storage.Neo4j;

/// <summary>
/// Defines a factory of pooled Neo4j drivers.
/// </summary>
public interface INeo4jDriverFactory
{
    /// <summary>
    /// Get the driver for a server and an authentication. There is exactly one driver, with its own connection pool,
    /// for every server and authentication.
    /// </summary>
    /// <param name="server">The bolt <see cref="Uri"/> of the server.</param>
    /// <param name="authentication">The <see cref="IAuthTokenManager"/> giving the driver its credentials.</param>
    /// <returns>The shared <see cref="IDriver"/>.</returns>
    IDriver Create(Uri server, IAuthTokenManager authentication);
}
