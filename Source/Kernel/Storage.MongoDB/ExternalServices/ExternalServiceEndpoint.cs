// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ExternalServices;

namespace Cratis.Chronicle.Storage.MongoDB.ExternalServices;

/// <summary>
/// Represents the MongoDB representation of an external service endpoint.
/// </summary>
public class ExternalServiceEndpoint
{
    /// <summary>
    /// Gets or sets the <see cref="ExternalServiceEndpointType"/>.
    /// </summary>
    public ExternalServiceEndpointType Type { get; set; }

    /// <summary>
    /// Gets or sets the HTTP configuration.
    /// </summary>
    public HttpEndpointConfiguration? Http { get; set; }

    /// <summary>
    /// Gets or sets the database configuration.
    /// </summary>
    public DatabaseEndpointConfiguration? Database { get; set; }
}
