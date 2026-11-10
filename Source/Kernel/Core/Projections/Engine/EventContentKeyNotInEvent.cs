// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine;

/// <summary>
/// The exception that is thrown when a dictionary key is read from an event property the event type does not have.
/// </summary>
/// <param name="propertyPath">The <see cref="PropertyPath"/> holding the event content key segment.</param>
/// <param name="eventType">The <see cref="EventType"/> the key is read from.</param>
/// <param name="keyPath">The <see cref="PropertyPath"/> within the event the key is read from.</param>
public class EventContentKeyNotInEvent(PropertyPath propertyPath, EventType eventType, PropertyPath keyPath)
    : Exception($"Property path '{propertyPath}' reads its dictionary key from '{keyPath}', which event type '{eventType}' does not have.");
