// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Images;

namespace Aksio.Cratis.Kernel;

public class TestClient
{
    public TestClient(KernelFixture fixture, string relativePath, string? executable = null)
    {
        var dockerBuildContextPath = DirectoryHelper.GetDirectoryThatHasSubDirectory(".git");
        var testClientsPath = DirectoryHelper.GetDirectoryThatHasSubDirectory("TestClients");

        if (executable is null)
        {
            var lastSegmentStart = relativePath.LastIndexOfAny(new[] { '/', '\\' });
            if (lastSegmentStart > 0) lastSegmentStart++;
            if (lastSegmentStart == -1) lastSegmentStart = 0;
            executable = relativePath.Substring(lastSegmentStart);
        }

        var imageName = executable.ToLowerInvariant();
        ContainerImage = new ImageFromDockerfileBuilder()
            .WithName(imageName)
            .WithDockerfileDirectory(dockerBuildContextPath)
            .WithDockerfile("Dockerfile.TestClient")
            .WithBuildArgument("CLIENT_PATH", relativePath)
            .WithBuildArgument("CLIENT_EXECUTABLE", $"{executable}.dll")
            .Build();

        ContainerImage.CreateAsync().GetAwaiter().GetResult();

        var appSettingsPath = Path.Combine(testClientsPath, "TestClients", "appsettings.json");
        Container = new ContainerBuilder()
                .WithImage(imageName)
                .WithNetwork(fixture.Network)
                .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("Cratis Client Connected"))
                .WithEnvironment("ASPNETCORE_URLS", "http://+:9094")
                .WithBindMount(appSettingsPath, "/App/appsettings.json")
                .Build();
    }

    public IFutureDockerImage ContainerImage { get; }
    public IContainer Container { get; }

    public Task Start() => Container.StartAsync();

    public Task Stop() => Container.StopAsync();
}
