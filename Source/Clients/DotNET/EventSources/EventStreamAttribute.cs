// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSources;

/// <summary>
/// Declares an event stream on an <see cref="IEventSource"/> definition.
/// </summary>
/// <remarks>
/// The name becomes the event stream type written on events appended to the stream. The format of the event
/// stream id is left to the caller and is not declared.
/// </remarks>
/// <param name="name">The name of the stream.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
public sealed class EventStreamAttribute(string name) : Attribute
{
    /// <summary>
    /// Gets the name of the stream.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Gets or sets the description of the stream.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the dimensions that take part in concurrency checks for the stream. Falls back to the event source when none.
    /// </summary>
    public ConcurrencyDimensions Concurrency { get; set; } = ConcurrencyDimensions.None;
}
