// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage;
using Orleans.TestKit;

namespace Cratis.Chronicle.Occurrences.for_LifecycleOccurrences.given;

public class a_lifecycle_grain : for_LifecycleFanOut.given.a_fan_out
{
    protected readonly TestKitSilo _silo = new();
    protected LifecycleOccurrences _grain;
    protected readonly IOccurrenceQueue _queue = Substitute.For<IOccurrenceQueue>();

    async Task Establish()
    {
        var storage = Substitute.For<IStorage>();
        storage.GetEventStore(new("store")).Returns(_store);
        _silo.AddService(storage);
        _silo.AddProbe<IOccurrenceQueue>(_ => _queue);
        _grain = await _silo.CreateGrainAsync<LifecycleOccurrences>("store");
    }
}
