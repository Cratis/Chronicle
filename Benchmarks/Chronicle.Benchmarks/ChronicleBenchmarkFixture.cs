// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;

namespace Cratis.Chronicle.Benchmarks;

/// <summary>
/// Represents a fixture for the Chronicle benchmarks that runs the Chronicle container using TestContainers.
/// </summary>
public class ChronicleBenchmarkFixture : IAsyncDisposable
{
    const string CertificatePassword = "TestPassword123";
    static readonly int MongoDBPort = int.Parse(Environment.GetEnvironmentVariable("BENCH_MONGO_PORT") ?? "27018");
    static readonly int ChroniclePort = int.Parse(Environment.GetEnvironmentVariable("BENCH_CHRONICLE_PORT") ?? "35001");
    readonly string _certificatePath;
    readonly string _backupsPath;

    INetwork? _network;
    IContainer? _container;
    bool _started;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChronicleBenchmarkFixture"/> class with private temporary paths.
    /// </summary>
    public ChronicleBenchmarkFixture()
    {
        var instance = Guid.NewGuid().ToString("N");
        var certificateDirectory = Path.Join(Path.GetTempPath(), $"chronicle-benchmark-4748-certs-{instance}");
        _certificatePath = Path.Combine(certificateDirectory, "chronicle-benchmark.pfx");
        BenchmarkCertificateGenerator.GenerateAndSaveCertificate(_certificatePath, CertificatePassword);

        // The bind mount source has to be a directory the container runtime can already see. A directory created
        // moments earlier below the benchmark host's working directory is not, because BenchmarkDotNet regenerates
        // that directory tree for every run, so the mount is anchored in a stable location instead.
        _backupsPath = Path.Join(Path.GetTempPath(), $"chronicle-benchmark-4748-backups-{instance}");
        Directory.CreateDirectory(_backupsPath);
    }

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
    /// Gets the Chronicle connection string for connecting.
    /// </summary>
    /// <remarks>
    /// TLS validation is skipped because the container serves a self-signed test certificate;
    /// this matches the integration fixtures and the built-in Development connection string.
    /// </remarks>
    public string ChronicleUrl => new ChronicleConnectionStringBuilder()
        .WithHost("localhost")
        .WithPort(ChroniclePort)
        .WithDevelopmentCredentials()
        .WithCertificate(_certificatePath, CertificatePassword)
        .WithTlsValidationSkipped()
        .Build();

    /// <summary>
    /// Starts the owned fixture within the caller's deadline.
    /// </summary>
    /// <param name="cancellationToken">The scenario deadline.</param>
    /// <returns>The startup task.</returns>
    public Task Prepare(CancellationToken cancellationToken) => InitializeAsync(cancellationToken);

    /// <summary>
    /// Reads the kernel container's complete stdout and stderr, for diagnosis before the container is disposed.
    /// </summary>
    /// <returns>The combined container log.</returns>
    public async Task<string> KernelLogs()
    {
        if (_container is null) return string.Empty;
        var logs = await _container.GetLogsAsync(timestampsEnabled: true);
        return $"{logs.Stdout}\n--- stderr ---\n{logs.Stderr}";
    }

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
        // Only the exact task-created certificate and empty private directories are removed; never recurse over backups.
        File.Delete(_certificatePath);
        Directory.Delete(Path.GetDirectoryName(_certificatePath)!, false);
        Directory.Delete(_backupsPath, false);
        GC.SuppressFinalize(this);
    }

    async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_started) return;

        _network = new NetworkBuilder()
            .WithName(Guid.NewGuid().ToString("D"))
            .Build();

        var imageName = Environment.GetEnvironmentVariable("CRATIS_CHRONICLE_LOCAL_IMAGE") ?? "cratis/chronicle:local-development";

        var waitStrategy = Wait.ForUnixContainer()
            .UntilInternalTcpPortIsAvailable(27017)
            .UntilInternalTcpPortIsAvailable(35000)
            .AddCustomWaitStrategy(new HttpsHealthWait(35000));

        var builder = new ContainerBuilder(imageName);

        // Opt-in diagnosis only: Debug logging of catch-up steps, reactor delivery and jobs changes timing.
        if (Environment.GetEnvironmentVariable("PROBE_KERNEL_DEBUG") == "1")
        {
            builder = builder
                .WithEnvironment("Logging__LogLevel__Cratis.Chronicle.Observation.Jobs", "Debug")
                .WithEnvironment("Logging__LogLevel__Cratis.Chronicle.Observation.Reactors", "Debug")
                .WithEnvironment("Logging__LogLevel__Cratis.Chronicle.Services.Observation.Reactors", "Debug")
                .WithEnvironment("Logging__LogLevel__Cratis.Orleans.Jobs", "Debug");
        }

        _container = builder
            .WithEnvironment("Storage__ConnectionDetails", "mongodb://localhost:27017/?maxPoolSize=500")
            .WithPortBinding(MongoDBPort, 27017)
            .WithPortBinding(ChroniclePort, 35000)
            .WithHostname("chronicle")
            .WithBindMount(_backupsPath, "/backups")
            .WithBindMount(_certificatePath, "/app/certs/chronicle.pfx")
            .WithEnvironment("Cratis__Chronicle__Tls__CertificatePath", "/app/certs/chronicle.pfx")
            .WithEnvironment("Cratis__Chronicle__Tls__CertificatePassword", CertificatePassword)
            .WithEnvironment("Cratis__Chronicle__EncryptionCertificate__CertificatePath", "/app/certs/chronicle.pfx")
            .WithEnvironment("Cratis__Chronicle__EncryptionCertificate__CertificatePassword", CertificatePassword)
            .WithNetwork(_network)
            .WithWaitStrategy(waitStrategy)
            .WithStartupCallback((container, ct) =>
            {
                Console.WriteLine($"Chronicle container {container.Id} started successfully");
                return Task.CompletedTask;
            })
            .Build();

        await StartContainerAsync(cancellationToken);
    }

    async Task StartContainerAsync(CancellationToken cancellationToken)
    {
        var container = _container;
        if (_started || container is null) return;

        var retryCount = 0;
        Exception? failure;
        do
        {
            try
            {
                var imageFullName = container.Image?.FullName ?? "[unknown]";
                Console.WriteLine($"Starting Chronicle container with image '{imageFullName}'...");
                failure = null;
                await container.StartAsync(cancellationToken);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to start the container (single startup attempt): {e.Message}");
                failure = e;
            }

            // Logs are only available once the container resource exists, so a container that never got created
            // must not take the retry loop down with it.
            try
            {
                var logs = await container.GetLogsAsync();
                Console.WriteLine(logs.Stdout);
                Console.WriteLine(logs.Stderr);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Unable to read the container logs: {e.Message}");
            }
        }
        while (failure is not null && ++retryCount < 1);

        if (failure is not null)
        {
            Console.WriteLine($"Failed to start the container after {retryCount} attempts.");
            throw failure;
        }

        _started = true;
        Console.WriteLine("Chronicle container started successfully.");
    }
}
