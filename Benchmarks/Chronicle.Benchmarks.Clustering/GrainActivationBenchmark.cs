// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;

using BenchmarkDotNet.Attributes;
using Cratis.Chronicle.Storage.Observation;
using SequenceNumber = KernelConcepts::Cratis.Chronicle.Concepts.Events.EventSequenceNumber;

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>Measures a single real grain read after a verified activation lifecycle.</summary>
/// <remarks>Job counts and Dry smoke selection come only from the existing runner's CLI.</remarks>
public class GrainActivationBenchmark : IAsyncDisposable
{
    GrainActivationFixture? _fixture;

    /// <summary>Gets or sets the lifecycle preceding the measured operation.</summary>
    [Params(GrainActivationMode.Warm, GrainActivationMode.Cold, GrainActivationMode.PostRestart)]
    public GrainActivationMode Mode { get; set; }

    /// <summary>Seeds the sequence-read workload.</summary>
    /// <returns>The setup task.</returns>
    [GlobalSetup(Target = nameof(FirstEventSequenceRead))]
    public Task SetupSequence() => Setup(sequence: true);

    /// <summary>Seeds the state-read workload.</summary>
    /// <returns>The setup task.</returns>
    [GlobalSetup(Target = nameof(FirstObserverStateRead))]
    public Task SetupObserver() => Setup(sequence: false);

    /// <summary>Awaits all lifecycle work before timing begins.</summary>
    [IterationSetup]
    public void Prepare() => _fixture!.Prepare().GetAwaiter().GetResult();

    /// <summary>Reads the next event-sequence number through the kernel grain.</summary>
    /// <returns>The retained next position.</returns>
    [Benchmark(OperationsPerInvoke = 1)]
    public Task<SequenceNumber> FirstEventSequenceRead() => _fixture!.ReadSequence();

    /// <summary>Reads the durable observer state through the kernel grain.</summary>
    /// <returns>The retained observer state.</returns>
    [Benchmark(OperationsPerInvoke = 1)]
    public Task<ObserverState> FirstObserverStateRead() => _fixture!.ReadObserver();

    /// <summary>Checks durability and activation identity after timing ends.</summary>
    [IterationCleanup]
    public void Verify() => _fixture!.Verify().GetAwaiter().GetResult();

    /// <summary>Releases the host and retained database, propagating teardown failures.</summary>
    /// <returns>The teardown task.</returns>
    [GlobalCleanup]
    public Task Cleanup() => DisposeAsync().AsTask();

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_fixture is not null) await _fixture.DisposeAsync();
        _fixture = null;
        GC.SuppressFinalize(this);
    }

    async Task Setup(bool sequence)
    {
        var artifacts = Environment.GetEnvironmentVariable("ACTIVATION_ARTIFACTS")
            ?? throw new InvalidOperationException("Set ACTIVATION_ARTIFACTS to the absolute run artifact directory.");
        _fixture = new(Mode, sequence, artifacts);
        try { await _fixture.Start(); }
        catch
        {
            await DisposeAsync();
            throw;
        }
    }
}
