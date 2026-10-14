// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventStoreSubscriptions;

/// <summary>
/// The exception that is thrown when a pinned subscription declaration cannot resolve an exact generation.
/// </summary>
/// <param name="eventTypeId">The unresolved event type identifier.</param>
public class EventStoreSubscriptionGenerationNotResolved(EventTypeId eventTypeId) : Exception($"Declare an exact generation for event type '{eventTypeId}' before requesting pinned subscription delivery.");
