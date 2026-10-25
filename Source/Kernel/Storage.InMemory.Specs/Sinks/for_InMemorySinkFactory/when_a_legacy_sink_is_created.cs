// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Chronicle.Storage.Sinks.for_Sinks;

namespace Cratis.Chronicle.Storage.InMemory.Sinks.for_InMemorySinkFactory;

public class when_a_legacy_sink_is_created : Specification
{
    IServiceProvider _services;
    ISink _sink;

    void Establish() => _services = Substitute.For<IServiceProvider>();
    void Because() => _sink = new InMemorySinkFactory(Substitute.For<ITypeFormats>(), _services).CreateFor("store", "namespace", GenerationContexts.Model(WellKnownSinkTypes.InMemory) with { ContainerName = "looks_like_a_generation_g2" });

    [Fact] void should_not_infer_a_binding_from_the_name() => _services.DidNotReceive().GetService(Arg.Any<Type>());
    [Fact] void should_keep_the_legacy_sink() => _sink.ShouldBeOfExactType<InMemorySink>();
}
