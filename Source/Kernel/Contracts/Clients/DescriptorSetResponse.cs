// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Clients;

/// <summary>
/// Represents a response containing the schema for all services.
/// </summary>
[ProtoContract]
public record DescriptorSetResponse
{
    /// <summary>
    /// Gets the schema definition as a string.
    /// </summary>
    [ProtoMember(1, IsRequired = true)]
    public string SchemaDefinition { get; init; } = string.Empty;
}
