// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventTypes;

namespace Cratis.Chronicle.Storage.EventTypes.for_EventTypeRegistry.when_reading;

public class the_same_unloaded_event_store_from_many_readers : given.a_registry
{
    const int Readers = 16;

    EventTypeSchema[] _schemas;
    int _readsForTheLoad;

    async Task Because()
    {
        var gate = new TaskCompletionSource();
        _storage.ReadGate = gate;
        var readers = Enumerable.Range(0, Readers).Select(_ => Task.Run(() => _registry.GetFor(_eventTypeId, 2))).ToArray();
        await Task.Delay(100);
        _storage.ReadGate = null;
        gate.SetResult();
        _schemas = await Task.WhenAll(readers);
        _readsForTheLoad = _storage.Reads;
    }

    [Fact] void should_load_the_event_store_once() => _readsForTheLoad.ShouldEqual(2);
    [Fact] void should_serve_every_reader_the_same_schema() => _schemas.All(schema => ReferenceEquals(schema, _schemas[0])).ShouldBeTrue();
}
