// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;

namespace Cratis.Chronicle.Benchmarks;

/// <summary>
/// Represents a fixture for the Chronicle benchmarks that runs the Chronicle container using TestContainers.
/// </summary>
public class ChronicleBenchmarkFixture : IAsyncDisposable
{
    const int MongoDBPort = 27018;
    const int ChroniclePort = 35001;

    INetwork? _network;
    IContainer? _container;
    bool _started;

    /// <summary>
    /// Gets the Chronicle container.
    /// </summary>
    public IContainer Container
    {
        get
        {
            if (_container is null)
            {
                InitializeAsync().GetAwaiter().GetResult();
            }
            return _container!;
        }
    }

    /// <summary>
    /// Gets the Chronicle URL for connecting.
    /// </summary>
    public string ChronicleUrl => $"chronicle://localhost:{ChroniclePort}";

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_container != null)
        {
            await _container.DisposeAsync();
        }
        if (_network != null)
        {
            await _network.DisposeAsync();
        }
        GC.SuppressFinalize(this);
    }

    async Task InitializeAsync()
    {
        if (_started) return;

        Directory.CreateDirectory("backups");

        _network = new NetworkBuilder()
            .WithName(Guid.NewGuid().ToString("D"))
            .Build();

        var imageName = Environment.GetEnvironmentVariable("CRATIS_CHRONICLE_LOCAL_IMAGE") ?? "cratis/chronicle:local-development";

        var waitStrategy = Wait.ForUnixContainer()
            .UntilInternalTcpPortIsAvailable(27017)
            .UntilHttpRequestIsSucceeded(req => req.ForPort(8080).ForPath("/health"));

        _container = new ContainerBuilder()
            .WithImage(imageName)
            .WithEnvironment("Storage__ConnectionDetails", $"mongodb://localhost:{MongoDBPort}")
            .WithPortBinding(MongoDBPort, 27017)
            .WithPortBinding(8081, 8080)
            .WithPortBinding(ChroniclePort, 35000)
            .WithHostname("chronicle")
            .WithBindMount(Path.Combine(Directory.GetCurrentDirectory(), "backups"), "/backups")
            .WithNetwork(_network)
            .WithWaitStrategy(waitStrategy)
            .WithStartupCallback((container, ct) =>
            {
                Console.WriteLine($"Chronicle container {container.Id} started successfully");
                return Task.CompletedTask;
            })
            .Build();

        await StartContainerAsync();
    }

    async Task StartContainerAsync()
    {
        if (_started || _container is null) return;

        var retryCount = 0;
        Exception? failure;
        do
        {
            try
            {
                Console.WriteLine($"Starting Chronicle container with image '{_container.Image.FullName}'...");
                failure = null;
                await _container.StartAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to start the container: {e.Message} - retrying...");
                failure = e;
                await Task.Delay(2000);
            }

            if (_container != null)
            {
                var logs = await _container.GetLogsAsync();
                Console.WriteLine(logs.Stdout);
                Console.WriteLine(logs.Stderr);
            }
        }
        while (failure is not null && ++retryCount < 10);

        if (failure is not null)
        {
            Console.WriteLine($"Failed to start the container after {retryCount} attempts.");
            throw failure;
        }
        else
        {
            _started = true;
            Console.WriteLine("Chronicle container started successfully.");
        }
    }
}
