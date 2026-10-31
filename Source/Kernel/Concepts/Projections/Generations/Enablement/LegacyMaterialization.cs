// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

/// <summary>
/// Describes existing data without claiming its producing revision.
/// </summary>
/// <param name="Projection">The Projection evidence.</param>
/// <param name="Namespace">The Namespace evidence.</param>
/// <param name="Container">The Container evidence.</param>
/// <param name="Observer">The Observer evidence.</param>
/// <param name="PositionEvidence">The PositionEvidence evidence.</param>
/// <param name="FailedPartitions">The FailedPartitions evidence.</param>
public record LegacyMaterialization(
    ProjectionId Projection,
    EventStoreNamespaceName Namespace,
    ReadModelContainerName Container,
    ObserverId Observer,
    string PositionEvidence,
    int FailedPartitions);
