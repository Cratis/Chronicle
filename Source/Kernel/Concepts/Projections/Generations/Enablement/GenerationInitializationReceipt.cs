// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

/// <summary>
/// Records the first successful completion of a step for a subject.
/// </summary>
/// <param name="EventStore">The EventStore evidence.</param>
/// <param name="Step">The Step evidence.</param>
/// <param name="Subject">The Subject evidence.</param>
/// <param name="Evidence">The Evidence evidence.</param>
/// <param name="Incarnation">The Incarnation evidence.</param>
/// <param name="FloorRevision">The FloorRevision evidence.</param>
/// <param name="RecordedAt">The RecordedAt evidence.</param>
[GenerateSerializer]
[Alias(nameof(GenerationInitializationReceipt))]
public record GenerationInitializationReceipt(
    [property: Id(0)] EventStoreName EventStore,
    [property: Id(1)] GenerationInitializationStep Step,
    [property: Id(2)] string Subject,
    [property: Id(3)] string Evidence,
    [property: Id(4)] StoreIncarnation Incarnation,
    [property: Id(5)] ulong FloorRevision,
    [property: Id(6)] DateTimeOffset RecordedAt);
