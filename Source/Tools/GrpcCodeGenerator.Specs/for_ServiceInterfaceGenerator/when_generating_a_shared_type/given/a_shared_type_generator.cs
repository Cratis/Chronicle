// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator.for_ServiceInterfaceGenerator.when_generating_a_shared_type.given;

/// <summary>
/// Base context for <see cref="ServiceInterfaceGenerator.GenerateSharedType"/> specs: configures
/// <see cref="SharedTypeRegistry"/> the same way a real run does (the folder path a shared type is written to
/// comes from the registry's namespace mapping, not from the generator alone) and creates a temporary output
/// directory, cleaned up after each spec.
/// </summary>
/// <remarks>
/// Leaf specs, not this context, carry <c>[Collection(SharedTypeRegistryCollection.Name)]</c> - xUnit does not
/// apply a collection attribute by inheritance, only from the concrete test class.
/// </remarks>
public class a_shared_type_generator : Specification
{
    protected ServiceInterfaceGenerator _generator = null!;
    protected string _outputDir = null!;

    void Establish()
    {
        SharedTypeRegistry.Configure(2, "Cratis.Chronicle.Contracts");
        _generator = new ServiceInterfaceGenerator(skipNamespaceSegments: 2, baseNamespace: "Cratis.Chronicle.Contracts");
        _outputDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_outputDir);
    }

    void Destroy()
    {
        if (Directory.Exists(_outputDir))
        {
            Directory.Delete(_outputDir, recursive: true);
        }
    }
}
