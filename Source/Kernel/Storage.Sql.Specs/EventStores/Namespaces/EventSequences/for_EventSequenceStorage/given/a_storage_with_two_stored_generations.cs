// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.given;

/// <summary>
/// An event at sequence number 0 stored with generation 1 ("original") and generation 2 ("migrated").
/// </summary>
public class a_storage_with_two_stored_generations : a_storage_for_appended_generation
{
    protected static readonly EventSourceId SourceId = "some-source";

    async Task Establish() => (await _storage.AppendMany([EventAt(0, 1)])).IsSuccess.ShouldBeTrue();

    protected Task ReviseAt(EventSequenceNumber sequenceNumber, uint generation, string value, string hash)
    {
        dynamic content = new ExpandoObject();
        content.value = value;
        return _storage.Revise(sequenceNumber, _eventType with { Generation = new EventTypeGeneration(generation) }, CorrelationId.NotSet, [], [], DateTimeOffset.UtcNow, (ExpandoObject)content, hash);
    }

    protected static string ValueOf(AppendedEvent @event) => (string)((IDictionary<string, object?>)@event.Content)["value"]!;
}
