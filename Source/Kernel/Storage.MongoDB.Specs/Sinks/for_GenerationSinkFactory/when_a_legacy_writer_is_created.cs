// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_GenerationSinkFactory;

public class when_a_legacy_writer_is_created : given.a_generation_factory
{
    ISink _sink;

    void Because() => _sink = _factory.CreateFor("store", "namespace", _context.GetReadModel("store", "namespace"));

    [Fact] void should_not_require_the_unregistered_generation_owner_services() => _sink.ShouldBeOfExactType<Sink>();
    [Fact] void should_preserve_unfenced_flag_off_writes() => Writes(_sink).ShouldBeOfExactType<UnfencedSinkWrites>();
    [Fact] void should_not_touch_a_gate() => _admissions.ShouldBeEmpty();
}
