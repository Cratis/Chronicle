// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts.Occurrences;
using Cratis.Chronicle.Schemas;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Occurrences.for_TriggerRegistry;

public class when_registering_without_application_occurrence_artifacts : given.an_occurrence_client
{
    void Establish()
    {
        var assembly = typeof(ChronicleClient).Assembly;
        var discovery = Substitute.For<ICanProvideAssembliesForDiscovery>();
        discovery.Assemblies.Returns([assembly]);
        discovery.DefinedTypes.Returns(assembly.DefinedTypes.Select(type => type.AsType()));
        var artifacts = new DefaultClientArtifactsProvider(discovery);
        _schemas.Generate(Arg.Any<Type>()).Returns(JsonSchema.FromJson("{}"));
        _registry = new(_store, artifacts, _schemas);
        ((IKernelCapabilities)_connection).Capabilities.Returns([]);
    }

    async Task Because() => _error = await Catch.Exception(async () =>
    {
        _registry.Discover();
        await _registry.Register();
    });

    [Fact] void should_allow_registration_against_an_older_kernel() => _error.ShouldBeNull();
    [Fact] void should_not_send_trigger_registration() => _service.DidNotReceive().RegisterTriggers(Arg.Any<RegisterTriggersRequest>(), Arg.Any<CallContext>());
}
