// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Images;
using MongoDB.Driver;

namespace Aksio.Cratis.Kernel;

public class TestClient
{
    public TestClient(string relativePath, string executable)
    {
        var imageName = Guid.NewGuid().ToString().Replace("-", string.Empty).ToLowerInvariant();

        var dockerBuildContextPath = GetDirectoryThatHasSubDirectory(".git");
        var testClientsPath = GetDirectoryThatHasSubDirectory("TestClients");

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
            .WithNetwork(GlobalFixture.Network)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("Cratis Client Connected"))
            .WithEnvironment("ASPNETCORE_URLS", "http://+:9094")
            .WithBindMount(appSettingsPath, "/App/appsettings.json")
            .Build();
    }

    public IFutureDockerImage ContainerImage { get; }
    public IContainer Container { get; }

    public Task Start() => Container.StartAsync();

    public Task Stop() => Container.StopAsync();

    string GetDirectoryThatHasSubDirectory(string subDirectoryToLookFor)
    {
        var current = Directory.GetCurrentDirectory();
        do
        {
            if (Directory.GetDirectories(current).Any(_ => _.EndsWith(subDirectoryToLookFor))) break;
            current = Directory.GetParent(current)?.FullName;
        } while (current != null);
        return current;
    }
}
