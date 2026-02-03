// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage;

/// <summary>
/// Exception that gets thrown when an invalid event store name is used.
/// </summary>
/// <param name="eventStoreName">The invalid event store name.</param>
/// <param name="reason">The reason why the name is invalid.</param>
public class InvalidEventStoreName(EventStoreName eventStoreName, string reason) : Exception($"Invalid event store name '{eventStoreName.Value}': {reason}")
{
    /// <summary>
    /// Gets the invalid event store name.
    /// </summary>
    public EventStoreName EventStoreName { get; } = eventStoreName;

    /// <summary>
    /// Gets the reason why the name is invalid.
    /// </summary>
    public string Reason { get; } = reason;
}
