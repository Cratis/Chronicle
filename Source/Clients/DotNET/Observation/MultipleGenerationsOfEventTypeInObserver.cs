// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// The exception that is thrown when an observer pins several generations of one event type.
/// </summary>
/// <param name="eventTypeId">The event type identifier.</param>
public class MultipleGenerationsOfEventTypeInObserver(EventTypeId eventTypeId) : Exception($"An observer cannot pin multiple generations of event type '{eventTypeId}'.");
