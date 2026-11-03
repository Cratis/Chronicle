// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Cratis.Arc.EntityFrameworkCore.Json;
using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Projections;

/// <summary>
/// Represents one immutable projection definition history row.
/// </summary>
public class ProjectionDefinitionHistoryRow
{
    /// <summary>Gets or sets the projection identifier.</summary>
    [MaxLength(200)]
    public ProjectionId ProjectionId { get; set; } = ProjectionId.Unspecified;

    /// <summary>Gets or sets the definition digest.</summary>
    [MaxLength(64)]
    public ProjectionDefinitionDigest Digest { get; set; } = new(string.Empty);

    /// <summary>Gets or sets the versioned digest algorithm.</summary>
    public string DigestAlgorithm { get; set; } = string.Empty;

    /// <summary>Gets or sets the first registering application version.</summary>
    public string? RegisteringVersion { get; set; }

    /// <summary>Gets or sets the first registration time.</summary>
    public DateTimeOffset RecordedAt { get; set; }

    /// <summary>Gets or sets the native serialized definitions, including pinned generations.</summary>
    [Json]
    public IDictionary<uint, string> Definitions { get; set; } = new Dictionary<uint, string>();
}
