// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ExternalServices;

namespace Cratis.Chronicle.ExternalServices;

/// <summary>
/// Defines a formatter that forms a connection string for a specific database external service endpoint type.
/// </summary>
/// <remarks>
/// New database providers are added by implementing this interface - it is discovered by convention,
/// keeping the connection-string formation open for extension.
/// </remarks>
public interface IFormDatabaseConnectionString
{
    /// <summary>
    /// Gets the <see cref="ExternalServiceEndpointType"/> this formatter supports.
    /// </summary>
    ExternalServiceEndpointType Type { get; }

    /// <summary>
    /// Forms a connection string from the given <see cref="DatabaseEndpointConfiguration"/>.
    /// </summary>
    /// <param name="configuration">The <see cref="DatabaseEndpointConfiguration"/> to form a connection string from.</param>
    /// <returns>The formed connection string.</returns>
    string Format(DatabaseEndpointConfiguration configuration);
}
