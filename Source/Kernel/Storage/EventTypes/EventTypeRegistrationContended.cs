// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.EventTypes;

/// <summary>
/// The exception that is thrown when repeated concurrent writes prevent conditional registration.
/// </summary>
/// <param name="eventTypeId">The event type being registered.</param>
public class EventTypeRegistrationContended(EventTypeId eventTypeId)
    : Exception($"Could not register event type '{eventTypeId}' after concurrent schema changes.");
