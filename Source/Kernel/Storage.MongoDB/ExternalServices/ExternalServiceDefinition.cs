// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ExternalServices;

namespace Cratis.Chronicle.Storage.MongoDB.ExternalServices;

/// <summary>
/// Represents the MongoDB representation of an external service definition.
/// </summary>
public class ExternalServiceDefinition
{
    /// <summary>
    /// Gets or sets the <see cref="ExternalServiceId"/> of the external service.
    /// </summary>
    public ExternalServiceId Id { get; set; } = ExternalServiceId.Unspecified;

    /// <summary>
    /// Gets or sets the <see cref="ExternalServiceName"/> of the external service.
    /// </summary>
    public ExternalServiceName Name { get; set; } = ExternalServiceName.Unspecified;

    /// <summary>
    /// Gets or sets the <see cref="ExternalServiceEndpoint"/>.
    /// </summary>
    public ExternalServiceEndpoint Endpoint { get; set; } = new();
}
