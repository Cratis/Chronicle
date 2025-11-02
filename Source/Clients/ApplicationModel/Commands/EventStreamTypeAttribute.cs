// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Applications.Commands;

/// <summary>
/// Attribute to specify the event stream type for a command.
/// </summary>
/// <param name="value">The event stream type value.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
public sealed class EventStreamTypeAttribute(string value) : Attribute
{
    /// <summary>
    /// Gets the event stream type value.
    /// </summary>
    public EventStreamType Value { get; } = value;
}
