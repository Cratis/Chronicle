// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events;

/// <summary>
/// Holds the unique identifiers for the built-in event types.
/// </summary>
public static class GlobalEventTypes
{
    /// <summary>
    /// Gets the unique identifier for the event type representing a redaction.
    /// </summary>
    public static readonly EventTypeId Redaction = "EventRedacted";
}
