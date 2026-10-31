// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.Packaging.for_the_packed_runtime_closure;

/// <summary>
/// The testing package is released as a stable package, so it cannot declare a prerelease dependency.
/// </summary>
/// <remarks>
/// NuGet fails the pack with NU5104 when a stable package depends on a prerelease. Declaring
/// OpenTelemetry.Instrumentation.GrpcNetClient, which the kernel pins at a beta, passed every pull request check and
/// then stopped the release from publishing any package.
/// </remarks>
public class when_checking_that_no_declared_package_is_a_prerelease : Specification
{
    const string TestingProject = "Source/Clients/Testing/Testing.csproj";

    IReadOnlyCollection<string> _prereleases;

    void Because() => _prereleases = RestoredDependencyGraph.DeclaredPrereleasePackages(ProjectFile(TestingProject));

    [Fact]
    void should_declare_no_prerelease_package() => string.Join(", ", _prereleases).ShouldEqual(string.Empty);

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
