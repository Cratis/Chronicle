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
/// <param name="SinkType">The resolved sink type.</param>
/// <param name="SinkConfiguration">The sink configuration.</param>
/// <remarks>
/// The sink is part of the materialization: a read model moved to another sink is a different materialization, so the new store
/// is never taken for an already materialized one. The sink is identified by its resolved type and name, never by its connection.
/// </remarks>
public record MaterializationKey(StoreIncarnation Incarnation, EventStoreNamespaceName Namespace, ProjectionId Projection, SinkTypeId SinkType, SinkConfigurationId SinkConfiguration)
{
    readonly SinkName _sinkName = SinkName.Default;

    /// <summary>
    /// Gets the resolved name of the sink configuration; <see cref="SinkName.Default"/> for the implicit default sink.
    /// </summary>
    public SinkName SinkName
    {
        get => _sinkName ?? SinkName.Default;
        init => _sinkName = value ?? SinkName.Default;
    }

    /// <summary>
    /// Gets the unambiguous escaped identity shared by bindings and gates.
    /// </summary>
    /// <remarks>
    /// A key for the implicit default sink has exactly the identity it had before sinks could be named, so existing bindings and
    /// gates keep their keys. A named sink adds its name, matched without regard to case, as a sixth segment.
    /// </remarks>
    public string Value => string.Join('/', Segments().Select(Uri.EscapeDataString));

    IEnumerable<string> Segments()
    {
        yield return Incarnation.Value.ToString("N");
        yield return Namespace.Value;
        yield return Projection.Value;
        yield return SinkType.Value;
        yield return SinkConfiguration.Value.ToString("N");
        if (!SinkName.IsDefault)
        {
            yield return SinkName.Value.Trim().ToLowerInvariant();
        }
    }
}
