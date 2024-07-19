// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Sinks;

namespace Cratis.Chronicle.Projections.Definitions;

/// <summary>
/// Represents the definition of where to store results from a projection.
/// </summary>
/// <param name="ConfigurationId">Unique <see cref="SinkConfigurationId"/> for the configuration.</param>
/// <param name="TypeId">Type of store.</param>
public record SinkDefinition(SinkConfigurationId ConfigurationId, SinkTypeId TypeId)
{
    /// <summary>
    /// Gets the none representation of <see cref="SinkDefinition"/>.
    /// </summary>
    public static readonly SinkDefinition None = new(SinkConfigurationId.None, SinkTypeId.None);
}
