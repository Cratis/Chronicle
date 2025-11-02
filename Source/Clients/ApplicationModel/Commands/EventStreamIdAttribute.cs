// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Applications.Commands;

/// <summary>
/// Attribute to specify the event stream id for a command.
/// </summary>
/// <param name="value">The event stream id value. Optional - if null, the value should be provided via ICanProvideEventStreamId interface.</param>
/// <param name="concurrency">Whether to include this metadata in the concurrency scope when appending events. Default is false.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
public sealed class EventStreamIdAttribute(string? value = null, bool concurrency = false) : Attribute
{
    /// <summary>
    /// Gets the event stream id value.
    /// </summary>
    public EventStreamId Value { get; } = value ?? EventStreamId.NotSet;

    /// <summary>
    /// Gets a value indicating whether this metadata should be included in the concurrency scope.
    /// </summary>
    public bool Concurrency { get; } = concurrency;
}
