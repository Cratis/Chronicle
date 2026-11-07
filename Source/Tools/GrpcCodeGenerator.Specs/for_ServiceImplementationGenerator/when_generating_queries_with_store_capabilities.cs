// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using TestAssembly.StoreCapabilities;

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator.for_ServiceImplementationGenerator;

public class when_generating_queries_with_store_capabilities : given.a_generated_service
{
    string _source = string.Empty;

    void Establish()
    {
        _serviceDefinition = new ServiceDefinition("Capabilities", "TestAssembly.StoreCapabilities");
        _serviceDefinition.Queries.Add(new QueryDefinition(typeof(CapabilityReadModel), [new QueryMethodDefinition(typeof(CapabilityReadModel).GetMethod(nameof(CapabilityReadModel.ForStore))!)]));
        _serviceDefinition.Queries.Add(new QueryDefinition(typeof(PlainReadModel), [new QueryMethodDefinition(typeof(PlainReadModel).GetMethod(nameof(PlainReadModel.ForStore))!)]));
        _contractCode = new ServiceInterfaceGenerator(0, ContractsNamespace).Generate(_serviceDefinition, _outputDirectory);
    }

    void Because() => _source = File.ReadAllText(new ServiceImplementationGenerator(0, ContractsNamespace, ImplementationsNamespace).Generate(_serviceDefinition, _outputDirectory).Path);

    [Fact] void should_emit_the_capability_for_the_attributed_read_model() => Routing("CapabilityReadModel").ShouldContain("Capability: \"chronicle.test.read\"");
    [Fact] void should_not_emit_a_capability_for_other_read_models() => Routing("PlainReadModel").ShouldNotContain("Capability:");

    string Routing(string readModel)
    {
        var start = _source.IndexOf($"{readModel}.ForStore", StringComparison.Ordinal);
        return _source[_source.LastIndexOf("public ", start, StringComparison.Ordinal)..start];
    }
}
