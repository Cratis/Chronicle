// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSources;

/// <summary>
/// Names an <see cref="IEventSource"/> definition.
/// </summary>
/// <remarks>
/// The name becomes the event source type written on every event appended through the definition.
/// </remarks>
/// <param name="name">Optional name of the event source. Defaults to the name of the type without a trailing "EventSource".</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class EventSourceAttribute(string? name = null) : Attribute
{
    /// <summary>
    /// Gets the explicit name of the event source, if any.
    /// </summary>
    public string? Name { get; } = name;

    /// <summary>
    /// Gets or sets the description of the event source.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the default dimensions that take part in concurrency checks for the event source.
    /// </summary>
    public ConcurrencyDimensions Concurrency { get; set; } = ConcurrencyDimensions.None;
}
