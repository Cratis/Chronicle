// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Applications.Commands;

/// <summary>
/// Attribute to specify the event source type for a command.
/// </summary>
/// <param name="value">The event source type value.</param>
/// <param name="concurrency">Whether to include this metadata in the concurrency scope when appending events. Default is false.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
public sealed class EventSourceTypeAttribute(string value, bool concurrency = false) : Attribute
{
    /// <summary>
    /// Gets the event source type value.
    /// </summary>
    public EventSourceType Value { get; } = value;

    /// <summary>
    /// Gets a value indicating whether this metadata should be included in the concurrency scope.
    /// </summary>
    public bool Concurrency { get; } = concurrency;
}
