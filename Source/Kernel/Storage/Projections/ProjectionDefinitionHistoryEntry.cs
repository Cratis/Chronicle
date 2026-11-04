// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Storage.Projections;

/// <summary>
/// Represents the first recorded registration of a distinct projection definition's content.
/// </summary>
/// <param name="Projection">The projection identifier.</param>
/// <param name="Digest">The definition content digest.</param>
/// <param name="DigestAlgorithm">The versioned digest algorithm.</param>
/// <param name="RegisteringVersion">The registering application's version, if supplied.</param>
/// <param name="RecordedAt">When the content was first recorded.</param>
/// <param name="Definition">The original accepted definition, including its registration metadata.</param>
public record ProjectionDefinitionHistoryEntry(
    ProjectionId Projection,
    ProjectionDefinitionDigest Digest,
    string DigestAlgorithm,
    string? RegisteringVersion,
    DateTimeOffset RecordedAt,
    ProjectionDefinition Definition);
