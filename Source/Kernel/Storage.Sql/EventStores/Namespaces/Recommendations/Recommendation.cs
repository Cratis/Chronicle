// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Recommendations;

/// <summary>
/// Represents a recommendation for event store namespace optimizations or improvements.
/// </summary>
[Table("Recommendations")]
public record Recommendation
{
    /// <summary>
    /// Gets the unique identifier for the recommendation.
    /// </summary>
    [Key]
    public Guid Id { get; init; }

    /// <summary>
    /// Gets the name of the recommendation.
    /// </summary>
    [StringLength(256)]
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the description providing details about the recommendation.
    /// </summary>
    [StringLength(1024)]
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Gets the type or category of the recommendation.
    /// </summary>
    [StringLength(512)]
    public string Type { get; init; } = string.Empty;

    /// <summary>
    /// Gets the date and time when the recommendation was created or occurred.
    /// </summary>
    public DateTimeOffset Occurred { get; init; }

    /// <summary>
    /// Gets the JSON representation of the request that generated this recommendation.
    /// </summary>
    public string RequestJson { get; init; } = string.Empty;
}
