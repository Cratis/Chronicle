// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// The exception that is thrown when one observer pins several generations of an event type.
/// </summary>
/// <param name="eventTypeId">The event type.</param>
public class ObserverPinsMultipleGenerationsOfEventType(EventTypeId eventTypeId) : Exception($"An observer cannot pin multiple generations of event type '{eventTypeId}'.");
