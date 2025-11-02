// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Applications.Commands;

/// <summary>
/// Attribute to specify the event source type for a command.
/// </summary>
/// <param name="value">The event source type value.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
public sealed class EventSourceTypeAttribute(string value) : Attribute
{
    /// <summary>
    /// Gets the event source type value.
    /// </summary>
    public EventSourceType Value { get; } = value;
}
