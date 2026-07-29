// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.ExternalServices;

/// <summary>
/// Represents the definition of a configured external service.
/// </summary>
[ProtoContract]
public class ExternalServiceDefinition
{
    /// <summary>
    /// Gets or sets the unique identifier of the external service.
    /// </summary>
    [ProtoMember(1)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the human-readable name of the external service.
    /// </summary>
    [ProtoMember(2)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the endpoint describing how to connect.
    /// </summary>
    [ProtoMember(3, IsRequired = true)]
    public ExternalServiceEndpoint Endpoint { get; set; } = new();
}
