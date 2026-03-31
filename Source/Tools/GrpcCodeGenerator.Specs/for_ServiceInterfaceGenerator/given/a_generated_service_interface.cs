// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator.for_ServiceInterfaceGenerator.given;

/// <summary>
/// Base context that discovers services from the test fixture assembly and creates a generator.
/// </summary>
public class a_generated_service_interface : Specification
{
    protected ServiceDefinition _serviceDefinition = null!;
    protected string _generatedCode = null!;
    protected ServiceInterfaceGenerator _generator = null!;

    void Establish()
    {
        var assembly = typeof(TestAssembly.Catalog.RegisterProduct).Assembly;
        var typeDiscovery = new TypeDiscovery(assembly);
        var services = typeDiscovery.DiscoverServices();
        _serviceDefinition = services.Values.First(s => s.ServiceName == "Products");
        _generator = new ServiceInterfaceGenerator(skipNamespaceSegments: 0, baseNamespace: "Generated");
    }
}
