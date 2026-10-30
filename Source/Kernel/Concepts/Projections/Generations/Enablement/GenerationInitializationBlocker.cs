// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

/// <summary>
/// Preserves an initialization refusal and its resolution.
/// </summary>
/// <param name="EventStore">The EventStore evidence.</param>
/// <param name="Kind">The Kind evidence.</param>
/// <param name="Subject">The Subject evidence.</param>
/// <param name="Detail">The Detail evidence.</param>
/// <param name="RaisedAt">The RaisedAt evidence.</param>
/// <param name="ResolvedAt">The ResolvedAt evidence.</param>
[GenerateSerializer]
[Alias(nameof(GenerationInitializationBlocker))]
public record GenerationInitializationBlocker(
    [property: Id(0)] EventStoreName EventStore,
    [property: Id(1)] GenerationInitializationBlockerKind Kind,
    [property: Id(2)] string Subject,
    [property: Id(3)] string Detail,
    [property: Id(4)] DateTimeOffset RaisedAt,
    [property: Id(5)] DateTimeOffset? ResolvedAt);
