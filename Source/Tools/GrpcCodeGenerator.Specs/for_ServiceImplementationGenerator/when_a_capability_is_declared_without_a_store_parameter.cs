// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using TestAssembly.StoreCapabilities;

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator.for_ServiceImplementationGenerator;

public class when_a_capability_is_declared_without_a_store_parameter : given.a_generated_service
{
    Exception? _exception;

    void Establish()
    {
        _serviceDefinition = new ServiceDefinition("Capabilities", "TestAssembly.StoreCapabilities");
        _serviceDefinition.Queries.Add(new QueryDefinition(typeof(CapabilityWithoutStore), [new QueryMethodDefinition(typeof(CapabilityWithoutStore).GetMethod(nameof(CapabilityWithoutStore.All))!)]));
        _contractCode = new ServiceInterfaceGenerator(0, ContractsNamespace).Generate(_serviceDefinition, _outputDirectory);
    }

    void Because() => _exception = Catch.Exception(() => new ServiceImplementationGenerator(0, ContractsNamespace, ImplementationsNamespace).Generate(_serviceDefinition, _outputDirectory));

    [Fact] void should_refuse_to_generate() => _exception.ShouldBeOfExactType<UnsupportedServiceShape>();
    [Fact] void should_name_the_read_model() => _exception!.Message.ShouldContain(nameof(CapabilityWithoutStore));
}
