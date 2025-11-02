// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Applications.Commands;

/// <summary>
/// Attribute to specify the event stream id for a command.
/// </summary>
/// <param name="value">The event stream id value.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
public sealed class EventStreamIdAttribute(string value) : Attribute
{
    /// <summary>
    /// Gets the event stream id value.
    /// </summary>
    public EventStreamId Value { get; } = value;
}
