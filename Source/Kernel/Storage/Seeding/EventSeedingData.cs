// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Seeding;

/// <summary>
/// Represents the seeding data stored per event store and namespace.
/// </summary>
/// <param name="EventStore">The event store name.</param>
/// <param name="Namespace">The namespace name.</param>
/// <param name="ByEventType">Events grouped by event type.</param>
/// <param name="ByEventSource">Events grouped by event source.</param>
public record EventSeedingData(
    EventStoreName EventStore,
    EventStoreNamespaceName Namespace,
    IDictionary<EventTypeId, IList<SeededEventEntry>> ByEventType,
    IDictionary<EventSourceId, IList<SeededEventEntry>> ByEventSource);
