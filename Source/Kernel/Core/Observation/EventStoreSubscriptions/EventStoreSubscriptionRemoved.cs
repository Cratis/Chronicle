// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions;

/// <summary>
/// Represents the event for an event store subscription that has been removed.
/// </summary>
[EventType, AllEventStores]
public record EventStoreSubscriptionRemoved;
