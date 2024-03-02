// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Sinks;

namespace Aksio.Cratis.Projections.Definitions;

/// <summary>
/// Represents the definition of where to store results from a projection.
/// </summary>
/// <param name="ConfigurationId">Unique <see cref="ProjectionSinkConfigurationId"/> for the configuration.</param>
/// <param name="TypeId">Type of store.</param>
public record ProjectionSinkDefinition(ProjectionSinkConfigurationId ConfigurationId, SinkTypeId TypeId);
