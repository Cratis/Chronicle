// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Projections.Definitions;

namespace Cratis.Chronicle.Storage.MongoDB.Projections;

/// <summary>
/// Represents one immutable projection definition history document.
/// </summary>
public record ProjectionDefinitionHistoryDocument
{
    /// <summary>Gets the structural projection and digest identity.</summary>
    public required ProjectionDefinitionHistoryKey Id { get; init; }

    /// <summary>Gets the versioned digest algorithm.</summary>
    public required string DigestAlgorithm { get; init; }

    /// <summary>Gets the first registering application version.</summary>
    public string? RegisteringVersion { get; init; }

    /// <summary>Gets the first registration time.</summary>
    public DateTimeOffset RecordedAt { get; init; }

    /// <summary>Gets the full native definition snapshot.</summary>
    public required ProjectionDefinition Definition { get; init; }
}
