// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Benchmarks;

/// <summary>
/// Measures a fresh client's connection and registration until every observer is active and subscribed.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10, invocationCount: 1)]
public class ObserverRegistrationBenchmark : IDisposable, IAsyncDisposable
{
    const int ObserverCount = 50;
    ChronicleBenchmarkFixture? _fixture;
    ChronicleClient? _client;
    (Type Reactor, Type Reducer)[] _observers = [];

    /// <summary>
    /// Starts the container outside the timed operation and prepares distinct closed observer types.
    /// </summary>
    /// <exception cref="InvalidOperationException">There are not enough distinct observer types.</exception>
    [GlobalSetup]
    public void Setup()
    {
        _fixture = new ChronicleBenchmarkFixture();
        _ = _fixture.Container;
        Type[] markers = [typeof(byte), typeof(short), typeof(int), typeof(long), typeof(float), typeof(double), typeof(decimal), typeof(bool), typeof(char), typeof(string)];
        _observers = markers.SelectMany(left => markers.Select(right => (
                Reactor: typeof(RegistrationReactor<,>).MakeGenericType(left, right),
                Reducer: typeof(RegistrationReducer<,>).MakeGenericType(left, right))))
            .Take(ObserverCount).ToArray();
        if (_observers.Length != ObserverCount)
        {
            throw new InvalidOperationException("Not enough distinct observer types for this benchmark case.");
        }
    }

    /// <summary>
    /// Connects a fresh client and waits for all requested observers, without appending any events.
    /// </summary>
    /// <returns>A task completing only when every reactor and reducer is ready.</returns>
    [Benchmark]
    public async Task ConnectAndRegisterObservers()
    {
        var options = CreateOptions();
        var artifacts = new ObserverScopedClientArtifactsProvider();
        _client = new ChronicleClient(options, artifactsProvider: artifacts);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(120));
        var store = await _client.GetEventStore($"registration-{Guid.NewGuid():N}").WaitAsync(timeout.Token);
        await WaitForObservers(store, timeout.Token);
    }

    /// <summary>
    /// Disposes the client outside the timed operation, with one invocation per iteration.
    /// </summary>
    [IterationCleanup]
    public void CleanupClient()
    {
        _client?.Dispose();
        _client = null;
    }

    /// <summary>
    /// Cleans up the benchmark container.
    /// </summary>
    /// <returns>A task completing after cleanup.</returns>
    [GlobalCleanup]
    public Task Cleanup() => DisposeAsync().AsTask();

    /// <inheritdoc/>
    public void Dispose() => DisposeAsync().AsTask().GetAwaiter().GetResult();

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        CleanupClient();
        if (_fixture is not null)
        {
            await _fixture.DisposeAsync();
            _fixture = null;
        }
        GC.SuppressFinalize(this);
    }

    ChronicleOptions CreateOptions()
    {
        var options = new ChronicleOptions(new ChronicleConnectionString(_fixture!.ChronicleUrl), connectTimeout: 30);
        for (var index = 0; index < _observers.Length; index++)
        {
            options.ExplicitArtifacts.RegisterReactor(_observers[index].Reactor, $"registration-reactor-{index:D2}");
            options.ExplicitArtifacts.RegisterReducer(_observers[index].Reducer, $"registration-reducer-{index:D2}");
        }
        return options;
    }

    async Task WaitForObservers(IEventStore store, CancellationToken cancellationToken)
    {
        foreach (var (reactorType, reducerType) in _observers)
        {
            var reactor = store.Reactors.GetHandlerFor(reactorType);
            var reducer = store.Reducers.GetHandlerFor(reducerType);
            while (true)
            {
                var reactorState = await reactor.GetState().WaitAsync(cancellationToken);
                var reducerState = await reducer.GetState().WaitAsync(cancellationToken);
                if (reactorState.IsSubscribed && reactorState.RunningState == ObserverRunningState.Active &&
                    reducerState.IsSubscribed && reducerState.RunningState == ObserverRunningState.Active)
                {
                    break;
                }
                await Task.Delay(50, cancellationToken);
            }
        }
    }
}
