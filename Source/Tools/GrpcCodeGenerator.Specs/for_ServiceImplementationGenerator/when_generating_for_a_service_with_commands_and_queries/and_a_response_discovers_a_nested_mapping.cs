// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator.for_ServiceImplementationGenerator.when_generating_for_a_service_with_commands_and_queries;

public class and_a_response_discovers_a_nested_mapping : given.a_generated_service
{
    string _code = null!;

    void Establish()
    {
        _serviceDefinition = new("NestedProducts", "TestAssembly.Catalog");
        _serviceDefinition.Commands.Add(new(typeof(TestAssembly.Catalog.InspectNestedProduct)));
    }

    void Because()
    {
        var generated = new ServiceImplementationGenerator(0, ContractsNamespace, ImplementationsNamespace)
            .Generate(_serviceDefinition, _outputDirectory);
        _code = File.ReadAllText(generated.Path);
    }

    [Fact] void should_emit_the_outer_mapping() => _code.ShouldContain("ToInspectNestedProductResponse(");
    [Fact] void should_emit_the_discovered_mapping() => _code.ShouldContain("static global::Generated.TestAssembly.Catalog.ProductResponse ToProductResponse(");
    [Fact] void should_convert_the_nested_value() => _code.ShouldContain("Product = ToProductResponse(source.Product)");
}
