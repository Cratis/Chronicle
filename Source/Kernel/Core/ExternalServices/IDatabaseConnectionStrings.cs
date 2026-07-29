// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ExternalServices;

namespace Cratis.Chronicle.ExternalServices;

/// <summary>
/// Defines a system that forms connection strings for database external service endpoints.
/// </summary>
public interface IDatabaseConnectionStrings
{
    /// <summary>
    /// Forms the connection string for the given database <see cref="ExternalServiceEndpoint"/>.
    /// </summary>
    /// <param name="endpoint">The <see cref="ExternalServiceEndpoint"/> to form a connection string for.</param>
    /// <returns>The formed connection string.</returns>
    /// <exception cref="UnsupportedDatabaseEndpointType">Thrown when there is no formatter for the endpoint type.</exception>
    /// <exception cref="MissingDatabaseConfiguration">Thrown when the endpoint has no database configuration.</exception>
    string GetFor(ExternalServiceEndpoint endpoint);
}
