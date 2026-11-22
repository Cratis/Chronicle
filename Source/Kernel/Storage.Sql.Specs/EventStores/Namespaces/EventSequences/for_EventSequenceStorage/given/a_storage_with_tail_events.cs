// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.given;

/// <summary>
/// Stores events 0 to 4 where later events deliberately do not match earlier filters:
/// 0 = type A / source one, 1 = type B / source two, 2 = type A / source two, 3 = type B / source two, 4 = type B / source three.
/// </summary>
public class a_storage_with_tail_events : a_storage_for_appended_generation
{
    protected static readonly EventType _typeA = new("tail-type-a", EventTypeGeneration.First);
    protected static readonly EventType _typeB = new("tail-type-b", EventTypeGeneration.First);

    protected async Task AppendTailEvents() => await _storage.AppendMany(
    [
        Tail(0, _typeA, "source-one"),
        Tail(1, _typeB, "source-two"),
        Tail(2, _typeA, "source-two"),
        Tail(3, _typeB, "source-two"),
        Tail(4, _typeB, "source-three")
    ]);

    EventToAppendToStorage Tail(EventSequenceNumber number, EventType type, EventSourceId source) =>
        EventAt(number, 1) with { EventType = type, EventSourceId = source };
}
