// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;
using Cratis.Chronicle.Concepts.Sinks;

namespace Cratis.Chronicle.Concepts.Projections.Generations.Bindings;

/// <summary>
/// Represents the materialization key.
/// </summary>
/// <param name="Incarnation">The incarnation.</param>
/// <param name="Namespace">The namespace.</param>
/// <param name="Projection">The projection.</param>
/// <param name="SinkType">The sink type.</param>
/// <param name="SinkConfiguration">The sink configuration.</param>
public record MaterializationKey(StoreIncarnation Incarnation, EventStoreNamespaceName Namespace, ProjectionId Projection, SinkTypeId SinkType, SinkConfigurationId SinkConfiguration)
{
    /// <summary>
    /// Gets the unambiguous escaped identity shared by bindings and gates.
    /// </summary>
    public string Value => string.Join('/', new[] { Incarnation.Value.ToString("N"), Namespace.Value, Projection.Value, SinkType.Value, SinkConfiguration.Value.ToString("N") }.Select(Uri.EscapeDataString));
}
