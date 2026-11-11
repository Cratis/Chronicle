// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reactors;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Observation.Reactors.Clients;
using Cratis.Chronicle.Services.Observation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Benchmarks.Kernel;

/// <summary>
/// Measures the grain calls the client subscription keeper makes on an idle kernel.
/// </summary>
/// <remarks>
/// An in-process silo with in-memory storage subscribes <c language="bash">--count</c> reactor observers to one client connection, then
/// runs the keeper of every observation stream exactly as the gRPC reactors service does, for <c language="bash">--seconds</c> seconds,
/// with nothing appended. <c language="bash">--without-keeper</c> runs no keepers, for the CPU baseline of the same observers.
/// An incoming grain call filter counts every call to the keeper's grain methods. Run it with
/// <c language="bash">--keeper-calls</c>; see the README.
/// </remarks>
public static class KeeperCalls
{
    static readonly string[] _keeperMethods = ["IsSubscriptionIntact", "KeepSubscription", "IsClientTarget", "KnowsClient"];
    static readonly ConcurrentDictionary<string, long> _calls = new();
    static readonly ConcurrentDictionary<long, long> _callsPerSecond = new();
    static readonly Stopwatch _clock = new();

    /// <summary>
    /// Runs the measurement.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>The process exit code.</returns>
    public static async Task<int> Run(string[] args)
    {
        var count = ObserverFootprint.IntArgument(args, "--count", 1000);
        var seconds = ObserverFootprint.IntArgument(args, "--seconds", 30);

        using var host = ObserverFootprint.BuildHost(0, 0, silo => silo.AddIncomingGrainCallFilter(async context =>
        {
            if (_keeperMethods.Contains(context.MethodName))
            {
                _calls.AddOrUpdate(context.MethodName, 1, (_, calls) => calls + 1);
                _callsPerSecond.AddOrUpdate((long)_clock.Elapsed.TotalSeconds, 1, (_, calls) => calls + 1);
            }

            await context.Invoke();
        }));
        await host.StartAsync();
        await ObserverFootprint.ConnectClient(host);
        var grainFactory = host.Services.GetRequiredService<IGrainFactory>();
        var siloAddress = host.Services.GetRequiredService<ILocalSiloDetails>().SiloAddress;
        var options = host.Services.GetRequiredService<IOptions<ChronicleOptions>>().Value;
        var siloDepartures = host.Services.GetRequiredService<ISiloDepartures>();

        await ObserverFootprint.Activate(grainFactory, siloAddress, "kept", count);
        Console.WriteLine($"Client subscription keeper: {count} subscribed reactor observers, idle for {seconds} s, connectedClients.reviseIntervalSeconds {options.ConnectedClients.ReviseIntervalSeconds}, subscriptionCheckIntervalSeconds {options.ConnectedClients.SubscriptionCheckIntervalSeconds}");

        using var streams = new CancellationTokenSource();
        _calls.Clear();
        _callsPerSecond.Clear();
        _clock.Restart();
        using var process = Process.GetCurrentProcess();
        var processorTime = process.TotalProcessorTime;
        var stopwatch = Stopwatch.StartNew();
        var keepers = Enumerable.Range(0, args.Contains("--without-keeper") ? 0 : count).Select(index => Keep(grainFactory, options, siloDepartures, index, streams.Token)).ToArray();
        await Task.Delay(TimeSpan.FromSeconds(seconds));
        await streams.CancelAsync();
        await Task.WhenAll(keepers);
        stopwatch.Stop();
        process.Refresh();
        processorTime = process.TotalProcessorTime - processorTime;

        var elapsed = stopwatch.Elapsed.TotalSeconds;
        foreach (var method in _keeperMethods)
        {
            var calls = _calls.GetValueOrDefault(method);
            Console.WriteLine($"  {method}: {calls} calls, {calls / elapsed:F1} per second, {calls / elapsed / count * 60:F2} per observer per minute (run average)");
        }

        var total = _calls.Values.Sum();
        Console.WriteLine($"  all keeper calls: {total} calls, {total / elapsed:F1} per second over {elapsed:F1} s");
        var busiest = _callsPerSecond.IsEmpty ? default : _callsPerSecond.MaxBy(second => second.Value);
        Console.WriteLine($"  busiest second: {busiest.Value} calls, at {busiest.Key} s");

        // The first check of every subscription comes one revise interval after it is kept; from then on an intact
        // subscription is checked once per safety-net wait. The last full safety-net interval shows that steady state.
        var steadyFrom = (long)Math.Ceiling(elapsed) - options.ConnectedClients.SubscriptionCheckIntervalSeconds;
        if (steadyFrom > options.ConnectedClients.ReviseIntervalSeconds)
        {
            var steady = _callsPerSecond.Where(second => second.Key >= steadyFrom).Sum(second => second.Value);
            Console.WriteLine($"  steady state, last {options.ConnectedClients.SubscriptionCheckIntervalSeconds} s: {steady} calls, {(double)steady / options.ConnectedClients.SubscriptionCheckIntervalSeconds:F1} per second, {(double)steady / count:F2} per observer");
        }

        Console.WriteLine($"  process CPU while idle: {processorTime.TotalSeconds / elapsed:F3} cores");

        await host.StopAsync();
        return 0;
    }

    static Task Keep(IGrainFactory grainFactory, ChronicleOptions options, ISiloDepartures siloDepartures, int index, CancellationToken streamClosed)
    {
        var reactorId = new ReactorId($"kept-{index}");
        var reactor = grainFactory.GetGrain<IReactor>(new ConnectedObserverKey(reactorId.Value, ObserverFootprint.EventStore, ObserverFootprint.Namespace, EventSequenceId.Log, ObserverFootprint.Connection));
        var definition = new ReactorDefinition(
            reactorId,
            ReactorOwner.Client,
            EventSequenceId.Log,
            [new EventTypeWithKeyExpression(new EventType("footprint-event", EventTypeGeneration.First), PropertyExpression.NotSet)]);

        return ClientSubscriptionKeeper.KeepWhileConnected(
            () => ClientSubscriptionKeeper.KeepSubscription(reactor.IsSubscriptionIntact, () => reactor.KeepSubscription(definition)),
            SubscriptionCheckSchedule.From(options),
            siloDepartures,
            TimeProvider.System,
            ObserverFootprint.Connection,
            NullLogger.Instance,
            streamClosed);
    }
}
