// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

namespace Cratis.Chronicle.Storage.Generations.for_GenerationFloorExtensions;

public class when_admitting_an_unsupported_floor : Specification
{
    IEventStoreStorage _storage;
    Exception _writer;
    Exception _protocol;
    Exception _capability;
    void Establish()
    {
        _storage = Substitute.For<IEventStoreStorage>();
        _storage.EventStore.Returns(new EventStoreName("orders"));
        _storage.GenerationFloor.Get().Returns(new GenerationFloor(new(1), new(1), [GenerationCapability.FloorAdmission], new(Guid.NewGuid()), 1, DateTimeOffset.UtcNow, "spec", "explicit"));
    }
    async Task Because()
    {
        _writer = await Catch.Exception(() => _storage.EnsureAdmitted(new(new(1), new(0), new[] { GenerationCapability.FloorAdmission }.ToFrozenSet())));
        _protocol = await Catch.Exception(() => _storage.EnsureAdmitted(new(new(0), new(1), new[] { GenerationCapability.FloorAdmission }.ToFrozenSet())));
        _capability = await Catch.Exception(() => _storage.EnsureAdmitted(new(new(1), new(1), Array.Empty<GenerationCapability>().ToFrozenSet())));
    }
    [Fact] void should_check_writer_independently() => _writer.ShouldBeOfExactType<KernelBelowGenerationFloor>();
    [Fact] void should_check_storage_independently() => _protocol.ShouldBeOfExactType<KernelBelowGenerationFloor>();
    [Fact] void should_check_every_capability() => _capability.ShouldBeOfExactType<KernelBelowGenerationFloor>();
    [Fact] void should_not_advance_the_floor() => _storage.GenerationFloor.DidNotReceive().Advance(Arg.Any<GenerationFloorAdvance>());
}
