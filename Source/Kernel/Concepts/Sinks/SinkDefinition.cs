// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Sinks;

/// <summary>
/// Represents the definition of where to store results from a projection.
/// </summary>
/// <param name="Configuration">Unique <see cref="SinkConfigurationId"/> for the configuration.</param>
/// <param name="Type">Type of store.</param>
public record SinkDefinition(SinkConfigurationId Configuration, SinkTypeId Type)
{
    /// <summary>
    /// Gets the none representation of <see cref="SinkDefinition"/>.
    /// </summary>
    public static readonly SinkDefinition None = new(SinkConfigurationId.None, SinkTypeId.None);
}
