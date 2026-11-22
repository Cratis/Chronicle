// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using Cratis.Chronicle.Services.Host;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Client;
using ProtoBuf.Grpc.Server;

namespace Cratis.Chronicle.Benchmarks.Kernel;

/// <summary>
/// Measures what it takes to open many server-streaming calls from one client connection, as a client that
/// registers many observers does after a restart.
/// </summary>
/// <remarks>
/// A real Kestrel listener on loopback with the kernel's HTTP/2 limits (<see cref="KestrelHttp2Limits"/>) serves a
/// stream that sends one message and then stays open, which is what an idle observer stream is. A client with the
/// channel settings of the Chronicle .NET client opens <c language="bash">--count</c> of them at once and the run
/// reports how long establishment took and which gRPC status every failure carried. With
/// <c language="bash">--multiplexed</c> the same number of observers share one stream. Run it with
/// <c language="bash">--client-streams</c>; see the README.
/// </remarks>
public static class ClientStreams
{
    /// <summary>
    /// Runs the measurement.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>The process exit code.</returns>
    public static async Task<int> Run(string[] args)
    {
        var count = ObserverFootprint.IntArgument(args, "--count", 11000);
        var maxStreams = ObserverFootprint.IntArgument(args, "--max-streams", KestrelHttp2Limits.MaxStreamsPerConnection);
        var timeoutSeconds = ObserverFootprint.IntArgument(args, "--timeout-seconds", 120);
        var streams = args.Contains("--multiplexed") ? 1 : count;

        var builder = WebApplication.CreateSlimBuilder();
        builder.Logging.SetMinimumLevel(LogLevel.Warning);
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.Listen(IPAddress.Loopback, 0, listen => listen.Protocols = HttpProtocols.Http2);
            KestrelHttp2Limits.Apply(options.Limits.Http2, maxStreams);
        });
        builder.Services.AddCodeFirstGrpc();
        await using var app = builder.Build();
        app.MapGrpcService<ClientStreamsProbeService>();
        await app.StartAsync();
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();

        using var handler = new SocketsHttpHandler
        {
            PooledConnectionIdleTimeout = Timeout.InfiniteTimeSpan,
            KeepAlivePingDelay = TimeSpan.FromSeconds(60),
            KeepAlivePingTimeout = TimeSpan.FromSeconds(30),
            EnableMultipleHttp2Connections = !args.Contains("--single-connection")
        };
        using var channel = GrpcChannel.ForAddress(address, new GrpcChannelOptions { HttpHandler = handler });
        var probe = channel.CreateGrpcService<IClientStreamsProbe>();

        using var cancellation = new CancellationTokenSource();
        var failures = new ConcurrentDictionary<string, int>();
        var established = new ConcurrentBag<double>();
        var memoryBefore = GC.GetTotalMemory(true);
        var clock = Stopwatch.StartNew();
        var attempts = Enumerable.Range(0, streams).Select(_ => Open(probe, clock, established, failures, cancellation.Token)).ToArray();

        var settled = Task.WhenAll(attempts.Select(_ => _.Established));
        await settled.WaitAsync(TimeSpan.FromSeconds(timeoutSeconds)).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        var elapsed = clock.Elapsed;
        var memoryAfter = GC.GetTotalMemory(true);
        var timedOut = !settled.IsCompleted;
        var opened = established.Count;
        var times = established.Order().ToArray();

        Console.WriteLine($"mode: {(args.Contains("--multiplexed") ? "multiplexed (one stream)" : "one stream per observer")}");
        Console.WriteLine($"observers: {count}, streams attempted: {streams}, max streams per connection: {maxStreams}, client connections: {(args.Contains("--single-connection") ? "one" : "as many as needed")}");
        Console.WriteLine($"established: {opened}, failed: {failures.Values.Sum()}{(timedOut ? $", still pending after {timeoutSeconds} s: {streams - opened - failures.Values.Sum()}" : string.Empty)}");
        Console.WriteLine($"time until the run settled: {elapsed.TotalSeconds:F1} s");
        if (times.Length > 0)
        {
            Console.WriteLine($"establishment time per stream: p50 {Percentile(times, 0.5):F0} ms, p95 {Percentile(times, 0.95):F0} ms, max {times[^1]:F0} ms");
        }

        Console.WriteLine($"managed memory held by the open streams (client and server in this process): {(memoryAfter - memoryBefore) / 1024.0 / 1024.0:F1} MB, {(streams == 0 ? 0 : (memoryAfter - memoryBefore) / 1024.0 / Math.Max(1, opened)):F1} KB per established stream");
        foreach (var (reason, number) in failures.OrderByDescending(_ => _.Value))
        {
            Console.WriteLine($"failure: {number} x {reason}");
        }

        await cancellation.CancelAsync();
        await Task.WhenAll(attempts.Select(_ => _.Finished)).WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        await app.StopAsync();
        return opened == streams ? 0 : 1;
    }

    static double Percentile(double[] sorted, double fraction) => sorted[(int)Math.Min(sorted.Length - 1, Math.Floor(sorted.Length * fraction))];

    static (Task Established, Task Finished) Open(IClientStreamsProbe probe, Stopwatch clock, ConcurrentBag<double> established, ConcurrentDictionary<string, int> failures, CancellationToken cancellationToken)
    {
        var first = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var finished = Task.Run(async () =>
        {
            var started = clock.Elapsed;
            try
            {
                await using var enumerator = probe.Watch(new ClientStreamsProbeRequest(), new CallContext(new CallOptions(cancellationToken: cancellationToken))).GetAsyncEnumerator(cancellationToken);
                if (await enumerator.MoveNextAsync())
                {
                    established.Add((clock.Elapsed - started).TotalMilliseconds);
                    first.TrySetResult();
                    while (await enumerator.MoveNextAsync())
                    {
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (RpcException ex)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    failures.AddOrUpdate($"{ex.StatusCode}: {Trim(ex.Status.Detail)}", 1, (_, number) => number + 1);
                }
            }
            catch (Exception ex)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    failures.AddOrUpdate($"{ex.GetType().Name}: {Trim(ex.Message)}", 1, (_, number) => number + 1);
                }
            }
            finally
            {
                first.TrySetResult();
            }
        });
        return (first.Task, finished);
    }

    static string Trim(string text) => text.Length > 120 ? text[..120] : text;
}
