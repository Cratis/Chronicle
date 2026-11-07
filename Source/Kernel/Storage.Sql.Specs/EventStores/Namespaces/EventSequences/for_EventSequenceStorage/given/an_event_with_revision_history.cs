// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.given;

public class an_event_with_revision_history : an_event_sequence_storage
{
    protected AppendedEvent _read;
    protected EventEntry _entry;

    async Task Establish() => await Append(EventSequenceNumber.First, Content("original"), "hash-original");

    protected Task Revise(string value = "recorded") =>
        _storage.Revise(EventSequenceNumber.First, _eventType, CorrelationId.New(), [], [], DateTimeOffset.UtcNow, Content(value), $"hash-{value}");

    protected async Task Read()
    {
        _read = await _storage.GetEventAt(EventSequenceNumber.First);
        await using var context = CreateContext();
        _entry = await context.Events.SingleAsync();
    }

    protected async Task ReleasedRevise(uint generation = 1)
    {
        await using var context = CreateContext();
        var entry = await context.Events.SingleAsync();
        EventEntryConverter.UpdateContentForGeneration(entry, new(generation), Content("released"));
        EventEntryConverter.UpdateHashForGeneration(entry, new(generation), "hash-released");
        entry.RevisedGeneration = generation;
        await context.SaveChangesAsync();
    }

    protected static ExpandoObject Content(string value)
    {
        var content = new ExpandoObject();
        ((IDictionary<string, object?>)content)["value"] = value;
        return content;
    }
}
