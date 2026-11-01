// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.Packaging.for_the_packed_runtime_closure;

/// <summary>
/// The testing package embeds kernel projects without their package references, so it has to declare the packages
/// those assemblies were compiled against.
/// </summary>
/// <remarks>
/// An embedded project contributes its assembly but none of its package references to the nuspec. When the package
/// only reaches one of them transitively, a consumer can resolve a lower version than the assembly was built
/// against and fail to load it. Microsoft.Extensions.Telemetry.Abstractions 10.10.0 won over the 10.11.0 the embedded
/// Core and Grpc assemblies reference, and nothing in this repository saw it because every project here references
/// the packages directly.
/// </remarks>
public class when_checking_that_every_package_an_embedded_project_needs_is_declared : Specification
{
    const string TestingProject = "Source/Clients/Testing/Testing.csproj";

    IReadOnlyCollection<string> _undeclared;

    void Because()
    {
        var project = ProjectFile(TestingProject);
        _undeclared = RestoredDependencyGraph.Read(project)
            .UndeclaredPackagesOfEmbeddedProjects(RestoredDependencyGraph.PackedRuntimeAssemblies(project));
    }

    [Fact]
    void should_declare_every_package_an_embedded_project_needs() => string.Join(", ", _undeclared).ShouldEqual(string.Empty);

    static FileInfo ProjectFile(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, relativePath)))
        {
            directory = directory.Parent;
        }

        return new FileInfo(Path.Combine(directory!.FullName, relativePath));
    }
}
