// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sinks;

/// <summary>
/// Attribute used to store a read model or a graph in a named sink configured in the kernel's <c language="text">sinks</c> configuration.
/// </summary>
/// <remarks>
/// <para>
/// Put it on a read model type - model-bound, filled by a projection or by a reducer - or on a graph type: a graph projection,
/// or a node or an edge of a model-bound graph. Without it the read model stores to <see cref="ChronicleOptions.DefaultSinkName"/>,
/// or the graph to <see cref="ChronicleOptions.DefaultGraphSinkName"/>; when those are empty too, the kernel's default sink is used.
/// </para>
/// <para>
/// The kernel refuses a read model that names a sink it has no configuration for. A passive read model has no sink, so
/// <see cref="ReadModels.PassiveAttribute"/> cannot be combined with this attribute.
/// </para>
/// </remarks>
/// <param name="name">The name of the sink configuration.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
public sealed class SinkAttribute(string name) : Attribute
{
    /// <summary>
    /// Gets the name of the sink configuration.
    /// </summary>
    public string Name { get; } = name;
}
