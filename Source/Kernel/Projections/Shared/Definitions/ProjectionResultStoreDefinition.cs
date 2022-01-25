// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Projections.Definitions
{
    /// <summary>
    /// Represents the definition of where to store results from a projection.
    /// </summary>
    /// <param name="ConfigurationId">Unique <see cref="ProjectionResultStoreConfigurationId"/> for the configuration.</param>
    /// <param name="TypeId">Type of store.</param>
    public record ProjectionResultStoreDefinition(ProjectionResultStoreConfigurationId ConfigurationId, ProjectionResultStoreTypeId TypeId);
}
