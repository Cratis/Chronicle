// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Cratis.Arc.EntityFrameworkCore.Json;

namespace Cratis.Chronicle.Storage.Sql.EventStores.ExternalServices;

/// <summary>
/// Represents the registration of a single external service.
/// </summary>
public class ExternalServiceDefinition
{
    /// <summary>
    /// Gets or sets the unique identifier for the external service.
    /// </summary>
    [Key]
    public required string Id { get; set; }

    /// <summary>
    /// Gets or sets the human-readable name of the external service.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the <see cref="ExternalServiceEndpoint"/>.
    /// </summary>
    [Json]
    public ExternalServiceEndpoint Endpoint { get; set; } = new();
}
