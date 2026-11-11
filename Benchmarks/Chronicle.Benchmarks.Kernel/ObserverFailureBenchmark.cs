// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using BenchmarkDotNet.Attributes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventStore;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Services;
using Cratis.Chronicle.Setup;
using Cratis.Chronicle.Storage.InMemory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Configuration;

namespace Chronicle.Benchmarks.Kernel;

/// <summary>
/// Measures failure recording and state writes on a real, in-process observer activation.
/// Iteration setup keeps attempt history bounded and drains coalesced work outside the measurement.
/// </summary>
[MemoryDiagnoser]
public class ObserverFailureBenchmark
{
    IHost _host = null!;
    IObserver _observer = null!;
    ulong _handled;

    /// <summary>Gets or sets the observer write-coalescing interval in seconds.</summary>
    [Params(0, 1)]
    public int WriteCoalescingSeconds { get; set; }

    /// <summary>Starts the in-memory silo and registers the observer.</summary>
    [GlobalSetup]
    public async Task Setup()
    {
        var config = new KernelConfiguration
        {
            Observers = new()
            {
                WriteCoalescingInterval = WriteCoalescingSeconds,
                MaxRetryAttempts = 0,
                BackoffDelay = 3600,
                MaximumBackoffDelay = 3600
            }
        };
        _host = Host.CreateDefaultBuilder()
            .ConfigureLogging(logging => logging.ClearProviders().SetMinimumLevel(LogLevel.None))
            .UseOrleans(silo =>
            {
                silo.UseLocalhostClustering(siloPort: 11121, gatewayPort: 30010);
                silo.Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = "observer-failure-benchmark";
                    options.ServiceId = "observer-failure-benchmark";
                });
                silo.Configure<EndpointOptions>(options => options.AdvertisedIPAddress = IPAddress.Loopback);
                silo.AddMemoryGrainStorageAsDefault();
                silo.AddMemoryGrainStorage("PubSubStore");
                silo.UseInMemoryReminderService();
                silo.ConfigureServices(services =>
                {
                    services.AddSingleton<IOptions<KernelConfiguration>>(Options.Create(config));
                    services.AddSingleton<IOptionsSnapshot<KernelConfiguration>>(new BenchmarkOptionsSnapshot<KernelConfiguration>(config));
                    services.AddSingleton<IOptions<ConfigurationForObserver>>(_ => Options.Create(new ConfigurationForObserver(config)));
                    services.AddSingleton<IOptionsSnapshot<ConfigurationForObserver>>(_ => new BenchmarkOptionsSnapshot<ConfigurationForObserver>(new ConfigurationForObserver(config)));
                    services.AddSingleton<IOptions<ConfigurationForEventStore>>(_ => Options.Create(new ConfigurationForEventStore(config)));
                    services.AddSingleton<IOptionsSnapshot<ConfigurationForEventStore>>(_ => new BenchmarkOptionsSnapshot<ConfigurationForEventStore>(new ConfigurationForEventStore(config)));
                    services.AddBindingsByConvention();
                    services.AddInMemoryStorage();
                    services.AddGrainStorage("storage");
                    services.AddSerialization();
                });
            }).Build();
        await _host.StartAsync();
        var key = new ObserverKey(ObserverId.New(), (EventStoreName)"observer-failure-benchmark", EventStoreNamespaceName.Default, EventSequenceId.Log);
        _observer = _host.Services.GetRequiredService<IGrainFactory>().GetGrain<IObserver>(key);
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [new EventType("benchmark-event", 1)], SiloAddress.Zero, null, true);
    }

    /// <summary>Clears failure history before a first failure or state write.</summary>
    [IterationSetup(Targets = new[] { nameof(FirstFailure), nameof(StateWrite) })]
    public void Reset()
    {
        _observer.ClearFailedPartitions().GetAwaiter().GetResult();
        DrainCoalescedWork();
    }

    /// <summary>Seeds one attempt before measuring a repeated failure.</summary>
    [IterationSetup(Target = nameof(RepeatedFailure))]
    public void SeedFailure()
    {
        Reset();
        _observer.PartitionFailed("partition", 42UL, ["First failure"], string.Empty).GetAwaiter().GetResult();
        DrainCoalescedWork();
    }

    /// <summary>Records the first failure of a partition.</summary>
    [Benchmark]
    public Task FirstFailure() => _observer.PartitionFailed("partition", 42UL, ["Failure"], string.Empty);

    /// <summary>Records another attempt on an already failed partition.</summary>
    [Benchmark]
    public Task RepeatedFailure() => _observer.PartitionFailed("partition", 42UL, ["Repeated failure"], string.Empty);

    /// <summary>Persists observer progress through the state-write path.</summary>
    [Benchmark]
    public Task StateWrite() => _observer.SetHandledStats(++_handled);

    /// <summary>Stops and disposes the silo.</summary>
    [GlobalCleanup]
    public async Task Cleanup()
    {
        await _host.StopAsync();
        _host.Dispose();
    }

    void DrainCoalescedWork()
    {
        if (WriteCoalescingSeconds > 0)
        {
            Task.Delay(TimeSpan.FromSeconds(WriteCoalescingSeconds * 3)).GetAwaiter().GetResult();
        }
    }
}
