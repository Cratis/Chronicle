// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Storage.MongoDB.Projections;

/// <summary>
/// Represents the structural history identity, enforced by MongoDB's unique _id index.
/// </summary>
/// <param name="Projection">The projection identifier.</param>
/// <param name="Digest">The definition digest.</param>
public record ProjectionDefinitionHistoryKey(ProjectionId Projection, ProjectionDefinitionDigest Digest);
