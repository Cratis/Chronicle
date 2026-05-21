// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

namespace Cratis.Chronicle.Storage.Sql.Cluster.Security;

/// <summary>
/// Represents the EF entity for an OAuth scope.
/// </summary>
public class ScopeEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for the scope.
    /// </summary>
    [Key]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the scope name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the resources serialized as a JSON array of strings.
    /// </summary>
    public string? Resources { get; set; }

    /// <summary>
    /// Gets or sets additional properties serialized as a JSON string.
    /// </summary>
    public string? Properties { get; set; }
}
