// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.Packaging.for_the_packed_runtime_closure;

/// <summary>
/// The testing package packs some runtime assemblies into itself instead of declaring their packages, so it has to
/// carry what those assemblies depend on as well.
/// </summary>
/// <remarks>
/// A packed assembly brings none of its package's dependencies. When one of them is neither packed too nor reached
/// through a declared dependency, every scenario a consumer constructs fails in type discovery, because it loads the
/// packed assembly's types and cannot resolve the missing one. Nothing in this repository sees that: every project
/// here references the packages directly. Cratis.Screenplay taking a dependency on Cratis.Screenplay.Matching did
/// exactly this, and it surfaced only at publish time.
/// </remarks>
public class when_checking_that_every_dependency_of_a_packed_package_travels_with_it : Specification
{
    const string TestingProject = "Source/Clients/Testing/Testing.csproj";

    IReadOnlyCollection<string> _uncovered;

    void Because()
    {
        var project = ProjectFile(TestingProject);
        _uncovered = RestoredDependencyGraph.Read(project)
            .UncoveredDependenciesOfPackedPackages(RestoredDependencyGraph.PackedRuntimeAssemblies(project));
    }

    [Fact]
    void should_carry_every_dependency_of_a_packed_package() => string.Join(", ", _uncovered).ShouldEqual(string.Empty);

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
