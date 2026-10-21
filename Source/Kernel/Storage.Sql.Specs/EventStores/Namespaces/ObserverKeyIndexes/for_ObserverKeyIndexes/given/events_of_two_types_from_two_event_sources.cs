// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Keys;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.given;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ObserverKeyIndexes.for_ObserverKeyIndexes.given;

/// <summary>
/// Sets up an event sequence where one event source has appended an event of the first type and another one of the second.
/// </summary>
public class events_of_two_types_from_two_event_sources : an_event_sequence_storage
{
    protected static readonly EventType _firstType = new("11111111-1111-1111-1111-111111111111", EventTypeGeneration.First);
    protected static readonly EventType _secondType = new("22222222-2222-2222-2222-222222222222", EventTypeGeneration.First);
    protected static readonly EventSourceId _firstSource = "first-source";
    protected static readonly EventSourceId _secondSource = "second-source";
    protected ObserverKey _observerKey;

    void Establish()
    {
        _observerKey = new("observer", _eventStore, _namespace, _eventSequenceId);

        using var context = CreateContext();
        context.Events.Add(new EventEntry { SequenceNumber = 0, Type = _firstType.Id, EventSourceId = _firstSource });
        context.Events.Add(new EventEntry { SequenceNumber = 1, Type = _secondType.Id, EventSourceId = _secondSource });
        context.SaveChanges();
    }

    protected ObserverKeyIndexes IndexesFor(string @namespace) => new(_eventStore, @namespace, _database);

    protected static async Task<IEnumerable<string>> KeysOf(IObserverKeyIndex index)
    {
        var keys = new List<string>();
        await foreach (var key in index.GetKeys(EventSequenceNumber.First))
        {
            keys.Add(key.ToString());
        }

        return keys;
    }
}
