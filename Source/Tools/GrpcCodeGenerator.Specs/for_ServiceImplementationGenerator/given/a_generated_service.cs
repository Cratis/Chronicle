// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator.for_ServiceImplementationGenerator.given;

/// <summary>
/// Base context holding a service whose gRPC contract has already been generated.
/// </summary>
public class a_generated_service : Specification
{
    protected const string ContractsNamespace = "Generated";
    protected const string ImplementationsNamespace = "Cratis.Chronicle.Services";

    protected ServiceDefinition _serviceDefinition = null!;
    protected string _contractCode = null!;
    protected string _outputDirectory = null!;

    void Establish()
    {
        _outputDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_outputDirectory);

        var assembly = typeof(TestAssembly.Catalog.RegisterProduct).Assembly;
        _serviceDefinition = new TypeDiscovery(assembly).DiscoverServices().Values.First(_ => _.ServiceName == "Products");

        _contractCode = new ServiceInterfaceGenerator(skipNamespaceSegments: 0, baseNamespace: ContractsNamespace)
            .Generate(_serviceDefinition, _outputDirectory);
    }

    void Destroy()
    {
        if (Directory.Exists(_outputDirectory))
        {
            Directory.Delete(_outputDirectory, recursive: true);
        }
    }
}
