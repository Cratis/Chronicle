// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Options;
using Orleans.TestKit;

namespace Cratis.Chronicle.Occurrences.for_OccurrenceQueue.given;

public class a_queue_grain : for_OccurrenceDelivery.given.a_queue
{
    protected readonly TestKitSilo _silo = new();
    protected OccurrenceQueue _grain;

    async Task Establish()
    {
        var storage = Substitute.For<IStorage>();
        var store = Substitute.For<IEventStoreStorage>();
        storage.GetEventStore(new("store")).Returns(store);
        store.Occurrences.Returns(_storage);
        store.TriggerDefinitions.Returns(_definitions);
        _silo.AddService(storage);
        _silo.AddService<IOptions<ChronicleOptions>>(Options.Create(new ChronicleOptions()));
        _silo.AddService<TimeProvider>(_clock);
        _grain = await _silo.CreateGrainAsync<OccurrenceQueue>(JsonSerializer.Serialize(new OccurrenceQueueKey("store", null, _trigger.Value)));
    }
}
