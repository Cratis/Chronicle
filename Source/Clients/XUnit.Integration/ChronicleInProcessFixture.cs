// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;

namespace Cratis.Chronicle.XUnit.Integration;

/// <summary>
/// Represents a global fixture for the test runs only the MongoDB container.
/// </summary>
public class ChronicleInProcessFixture : ChronicleFixture
{
    /// <summary>
    /// Gets the name of the Mongo container.
    /// </summary>
    public const string HostName = "mongo";

    const string UriPrefix = "MONGODB_URI=";
    static readonly TimeSpan _mongoMemoryServerInstallationTimeout = TimeSpan.FromMinutes(5);
    static readonly TimeSpan _mongoMemoryServerStartupTimeout = TimeSpan.FromMinutes(2);
    static readonly SemaphoreSlim _npmInstallLock = new(1, 1);
    static readonly string _mongoMemoryServerDirectory = Path.Combine(Path.GetTempPath(), "chronicle-mongodb-memory-server");
    InMemoryMongoDBServer _mongoDBServer = default!;
    bool _useContainerFallback;

    /// <inheritdoc/>
    public override string MongoDBServer
    {
        get
        {
            EnsureMongoDBInitialized();
            if (_useContainerFallback)
            {
                return base.MongoDBServer;
            }
            return _mongoDBServer.MongoDBServer;
        }
    }

    /// <inheritdoc/>
    public override IContainer MongoDBContainer => _useContainerFallback ? base.MongoDBContainer : throw new InvalidOperationException("MongoDBContainer is not available when using the in-memory MongoDB server.");

    /// <inheritdoc/>
    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        if (!_useContainerFallback && _mongoDBServer is not null)
        {
            await _mongoDBServer.DisposeAsync();
        }
    }

    /// <inheritdoc/>
    protected override async Task InitializeMongoDB()
    {
        try
        {
            await EnsureMongoMemoryServerPackage();
            _mongoDBServer = await InMemoryMongoDBServer.Start(_mongoMemoryServerDirectory);
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Falling back to MongoDB container because in-memory startup failed: {exception.Message}");
            _useContainerFallback = true;
            await base.InitializeMongoDB();
        }
    }

    /// <inheritdoc/>
    protected override IContainer BuildContainer(INetwork network)
    {
        return new ContainerBuilder("mongo")
            .WithCommand("/bin/sh", "-c", "mongod --replSet rs0 --bind_ip_all > /proc/1/fd/1 2>/proc/1/fd/2 & until mongosh --quiet --eval 'db.adminCommand(\"ping\")' >/dev/null 2>&1; do sleep 0.1; done; mongosh --eval 'rs.initiate({_id:\"rs0\",members:[{_id:0,host:\"localhost:27017\"}]})' || true; tail -f /dev/null")
            .WithTmpfsMount("/data/db", AccessMode.ReadWrite)
            .WithPortBinding(27017, assignRandomHostPort: true)
            .WithHostname(HostName)
            .WithBindMount(Path.Combine(Directory.GetCurrentDirectory(), "backups"), "/backups")
            .WithNetwork(network)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilInternalTcpPortIsAvailable(27017)
                .UntilCommandIsCompleted("/bin/sh", "-c", "mongosh --quiet --eval 'rs.status().ok' | grep -q 1"))
            .Build();
    }

    static async Task EnsureMongoMemoryServerPackage()
    {
        if (Directory.Exists(Path.Combine(_mongoMemoryServerDirectory, "node_modules", "mongodb-memory-server")))
        {
            return;
        }

        await _npmInstallLock.WaitAsync();
        try
        {
            if (Directory.Exists(Path.Combine(_mongoMemoryServerDirectory, "node_modules", "mongodb-memory-server")))
            {
                return;
            }

            Directory.CreateDirectory(_mongoMemoryServerDirectory);

            await RunProcess(
                fileName: "npm",
                arguments: $"install --no-save --prefix \"{_mongoMemoryServerDirectory}\" mongodb-memory-server",
                _mongoMemoryServerDirectory,
                _mongoMemoryServerInstallationTimeout);
        }
        finally
        {
            _npmInstallLock.Release();
        }
    }

    static async Task RunProcess(string fileName, string arguments, string workingDirectory, TimeSpan timeout)
    {
        using var process = new Process
        {
            StartInfo = new()
            {
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false
            }
        };
        process.Start();
        var output = new StringBuilder();
        var error = new StringBuilder();
        process.OutputDataReceived += (_, args) =>
        {
            if (args.Data is not null)
            {
                output.AppendLine(args.Data);
            }
        };
        process.ErrorDataReceived += (_, args) =>
        {
            if (args.Data is not null)
            {
                error.AppendLine(args.Data);
            }
        };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        var waitForExitTask = process.WaitForExitAsync();
        var completedTask = await Task.WhenAny(waitForExitTask, Task.Delay(timeout));
        if (completedTask != waitForExitTask)
        {
            process.Kill(true);
            throw new TimeoutException($"Process '{fileName} {arguments}' timed out after {timeout}.");
        }

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Process '{fileName} {arguments}' failed with exit code {process.ExitCode}.{Environment.NewLine}stdout:{Environment.NewLine}{output}{Environment.NewLine}stderr:{Environment.NewLine}{error}");
        }
    }

    sealed class InMemoryMongoDBServer(Process process, string mongoDBServer) : IAsyncDisposable
    {
        public string MongoDBServer { get; } = mongoDBServer;

        public static async Task<InMemoryMongoDBServer> Start(string workingDirectory)
        {
            var scriptPath = Path.Combine(workingDirectory, "start-mongodb-memory-server.cjs");
            var scriptContent = string.Join('\n', [
                "const { MongoMemoryReplSet } = require('mongodb-memory-server');",
                string.Empty,
                "(async () => {",
                "    const replSet = await MongoMemoryReplSet.create({",
                "        replSet: { count: 1, storageEngine: 'wiredTiger' }",
                "    });",
                string.Empty,
                "    const uri = replSet.getUri();",
                "    console.log(`MONGODB_URI=${uri}`);",
                string.Empty,
                "    const stop = async () => {",
                "        await replSet.stop();",
                "        process.exit(0);",
                "    };",
                string.Empty,
                "    process.on('SIGTERM', stop);",
                "    process.on('SIGINT', stop);",
                string.Empty,
                "    await new Promise(() => {});",
                "})().catch(error => {",
                "    console.error(error);",
                "    process.exit(1);",
                "});",
                string.Empty
            ]);
            await File.WriteAllTextAsync(scriptPath, scriptContent);

            var output = new StringBuilder();
            var errors = new StringBuilder();
            var uriReady = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

#pragma warning disable CA2000
            var process = new Process
            {
                StartInfo = new()
                {
                    FileName = "node",
                    Arguments = $"\"{scriptPath}\"",
                    WorkingDirectory = workingDirectory,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                }
            };
#pragma warning restore CA2000

            process.OutputDataReceived += (_, args) =>
            {
                if (args.Data is null) return;
                output.AppendLine(args.Data);
                if (args.Data.StartsWith(UriPrefix, StringComparison.Ordinal))
                {
                    uriReady.TrySetResult(args.Data[UriPrefix.Length..]);
                }
            };
            process.ErrorDataReceived += (_, args) =>
            {
                if (args.Data is not null)
                {
                    errors.AppendLine(args.Data);
                }
            };

            try
            {
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                var completedTask = await Task.WhenAny(uriReady.Task, Task.Delay(_mongoMemoryServerStartupTimeout));
                if (completedTask == uriReady.Task)
                {
                    var mongoDBServer = await uriReady.Task;
                    return new InMemoryMongoDBServer(process, mongoDBServer);
                }

                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
                process.Dispose();
                throw new TimeoutException($"Timed out waiting for in-memory MongoDB server startup.{Environment.NewLine}stdout:{Environment.NewLine}{output}{Environment.NewLine}stderr:{Environment.NewLine}{errors}");
            }
            catch
            {
                process.Dispose();
                throw;
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (process.HasExited)
            {
                process.Dispose();
                return;
            }

            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
            process.Dispose();
        }
    }
}
