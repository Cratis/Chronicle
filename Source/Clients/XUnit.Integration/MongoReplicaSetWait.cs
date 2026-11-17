// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;

namespace Cratis.Chronicle.XUnit.Integration;

/// <summary>
/// Waits for a writable single-node replica set and retains evidence when startup stalls.
/// </summary>
/// <param name="timeProvider">The clock used for the readiness deadline.</param>
internal sealed class MongoReplicaSetWait(TimeProvider? timeProvider = null) : IWaitUntil
{
    /// <summary>
    /// Starts MongoDB and records replica-set initialization, accepting AlreadyInitialized (code 23).
    /// </summary>
    internal const string StartupCommand = "mongod --replSet rs0 --bind_ip_all > /proc/1/fd/1 2>/proc/1/fd/2 & " +
        "until mongosh --quiet --eval 'db.adminCommand(\"ping\")' >/dev/null 2>&1; do sleep 0.1; done; " +
        "mongosh --quiet --eval 'try { " +
        "const result = rs.initiate({_id:\"rs0\",members:[{_id:0,host:\"localhost:27017\"}]}); printjson(result); " +
        "if (result.ok !== 1 && result.code !== 23) quit(1); " +
        "} catch (error) { print(error); if (error.code !== 23) quit(1); }' > /tmp/chronicle-mongo-init.log 2>&1; " +
        "init_status=$?; echo \"MongoDB rs.initiate exit code: $init_status\" >> /tmp/chronicle-mongo-init.log; " +
        "cat /tmp/chronicle-mongo-init.log; tail -f /dev/null";

    /// <summary>
    /// Emits initialization and replica-set status, succeeding only for the writable primary.
    /// </summary>
    internal const string ReadinessCommand = "cat /tmp/chronicle-mongo-init.log 2>/dev/null; " +
        "exec mongosh --quiet --eval 'const status = rs.status(); printjson(status); " +
        "if (status.ok !== 1 || status.myState !== 1) quit(1);'";

    /// <summary>
    /// Allows slow CI hosts 150 seconds to initialize/elect a primary, reserving time for diagnostics.
    /// </summary>
    internal static readonly TimeSpan ReadinessTimeout = TimeSpan.FromSeconds(150);

    /// <summary>
    /// Bounds the strategy, including its final bounded probe and log collection, to three minutes.
    /// </summary>
    internal static readonly TimeSpan StrategyTimeout = TimeSpan.FromMinutes(3);
    static readonly TimeSpan _probeTimeout = TimeSpan.FromSeconds(10);
    static readonly TimeSpan _logsTimeout = TimeSpan.FromSeconds(5);
    readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    long? _started;
    string _lastOutput = "No readiness probe completed.";

    /// <inheritdoc/>
    public async Task<bool> UntilAsync(IContainer container)
    {
        _started ??= _timeProvider.GetTimestamp();
        if (_timeProvider.GetElapsedTime(_started.Value) >= ReadinessTimeout)
        {
            // Deliberately not TimeoutException: ChronicleFixture retries that exception ten times.
            // A fresh Mongo container is not a safe repair for an unexplained initialization failure.
            throw new MongoReplicaSetReadinessFailed(
                $"MongoDB replica-set readiness timed out after {ReadinessTimeout.TotalSeconds} seconds. " +
                $"Container={container.Id}, Image={container.Image.FullName}\n" +
                $"Last readiness command output (rs.status()):\n{_lastOutput}\n" +
                $"Container logs (tail):\n{await ReadLogs(container)}");
        }
        return await Probe(container);
    }

    static async Task<string> ReadLogs(IContainer container)
    {
        using var cancellation = new CancellationTokenSource(_logsTimeout);
        try
        {
            var logs = await container.GetLogsAsync(timestampsEnabled: false, ct: cancellation.Token).WaitAsync(cancellation.Token);
            return Tail(logs.Stdout + "\n" + logs.Stderr);
        }
        catch (Exception exception)
        {
            return $"[container logs unavailable: {exception.GetType().Name}]";
        }
    }

    static string Tail(string output)
    {
        var lines = string.Join('\n', output.Split('\n').TakeLast(60));
        return lines.Length <= 16000 ? lines : lines[^16000..];
    }

    async Task<bool> Probe(IContainer container)
    {
        using var cancellation = new CancellationTokenSource(_probeTimeout);
        try
        {
            var result = await container.ExecAsync(["/bin/sh", "-c", ReadinessCommand], cancellation.Token).WaitAsync(cancellation.Token);
            _lastOutput = $"ExitCode={result.ExitCode}\n{Tail(result.Stdout + "\n" + result.Stderr)}";
            return result.ExitCode == 0;
        }
        catch (Exception exception)
        {
            // Keep the last completed rs.status() output even when the Docker exec transport stalls.
            _lastOutput = Tail($"{_lastOutput}\nReadiness probe failed: {exception.GetType().Name}");
            return false;
        }
    }
}
