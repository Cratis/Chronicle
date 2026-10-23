// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

namespace Cratis.Chronicle.Storage.InMemory.Generations.for_GenerationFloorStorage;

public class when_lowering_a_floor : given.a_floor_storage
{
    GenerationFloorAdvanceResult _writer;
    GenerationFloorAdvanceResult _storageProtocol;
    GenerationFloorAdvanceResult _capabilities;
    async Task Establish() => await _storage.Advance(Request());
    async Task Because()
    {
        _writer = await _storage.Advance(Request(_floor with { MinimumWriterProtocol = new(0), MinimumStorageProtocol = new(2) }, 1));
        _storageProtocol = await _storage.Advance(Request(_floor with { MinimumWriterProtocol = new(2), MinimumStorageProtocol = new(0) }, 1));
        _capabilities = await _storage.Advance(Request(new(new(2), new(2), [], _floor.Incarnation, 0, DateTimeOffset.MinValue, "", ""), 1));
    }
    [Fact] void should_not_trade_writer_for_storage() => _writer.Value.ShouldBeOfExactType<GenerationFloorWouldLower>();
    [Fact] void should_not_trade_storage_for_writer() => _storageProtocol.Value.ShouldBeOfExactType<GenerationFloorWouldLower>();
    [Fact] void should_not_drop_a_capability() => _capabilities.Value.ShouldBeOfExactType<GenerationFloorWouldLower>();
    [Fact] async Task should_not_change_history() => (await _storage.GetHistory()).Count.ShouldEqual(1);
}
