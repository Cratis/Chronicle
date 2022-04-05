// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Projections.Definitions;

/// <summary>
/// Represents the definition of a projection pipeline.
/// </summary>
/// <param name="ProjectionId">Projection it is for.</param>
/// <param name="ProjectionEventProviderTypeId">Type of event provider.</param>
/// <param name="ResultStores">Collection of <see cref="ProjectionResultStoreDefinition"/>.</param>
public record ProjectionPipelineDefinition(ProjectionId ProjectionId, ProjectionEventProviderTypeId ProjectionEventProviderTypeId, IEnumerable<ProjectionResultStoreDefinition> ResultStores);
