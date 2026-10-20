// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;
using Grpc.Core;

namespace Cratis.Chronicle.Integration.SqlClustering;

public sealed class ClusterLoad : IAsyncDisposable
{
    readonly ConcurrentQueue<Guid> _observed = new();
    readonly ConcurrentQueue<(Guid Id, EventSequenceNumber Sequence)> _acknowledged = new();
    readonly CancellationTokenSource _stop = new();
    readonly string _source = Guid.NewGuid().ToString();
    ChronicleClient _client = null!;
    IEventStore _store = null!;
    IReactorHandler _reactor = null!;
    Task _load = Task.CompletedTask;
    int _failures;

    public int Successes => _acknowledged.Count;

    public async Task Start(SqlCluster cluster)
    {
        var options = new ChronicleOptions(cluster.ConnectionString) { AutoDiscoverAndRegister = false, ConnectTimeout = 30 };
        options.ExplicitArtifacts.RegisterEventType<ClusterEventAppended>();
        _client = new(options);
        _store = await _client.GetEventStore("sql-clustering");
        _reactor = await _store.Reactors.Register(
            "cluster-integrity",
            builder => builder.WithEventType(_store.EventTypes.GetEventTypeFor(typeof(ClusterEventAppended))),
            (@event, _) =>
            {
                _observed.Enqueue(@event.Content["id"]!.GetValue<Guid>());
                return Task.CompletedTask;
            });
        await _reactor.WaitTillActive(TimeSpan.FromSeconds(60));
        _load = Run();
    }

    public async Task WaitForProgress(int minimum)
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(90));
        using var poll = new PeriodicTimer(TimeSpan.FromMilliseconds(100));
        while (Successes < minimum)
        {
            if (_load.IsCompleted)
            {
                await _load;
                throw new InvalidOperationException("Append load ended before making progress.");
            }

            await poll.WaitForNextTickAsync(deadline.Token);
        }
    }

    public async Task StopAndVerify()
    {
        await _stop.CancelAsync();
        await _load;
        var acknowledged = _acknowledged.ToArray();
        (acknowledged.Length >= 20).ShouldBeTrue();
        await _reactor.WaitTillReachesEventSequenceNumber(acknowledged[^1].Sequence, TimeSpan.FromSeconds(90));
        var stored = await _store.EventLog.GetForEventSourceIdAndEventTypes(_source, []);
        Console.WriteLine($"Acknowledged={acknowledged.Length}, failed={_failures}, stored={stored.Count}, observed={_observed.Count}");
        stored.Select(@event => ((ClusterEventAppended)@event.Content).Id).ShouldContainOnly(acknowledged.Select(append => append.Id));
        stored.Count.ShouldEqual(acknowledged.Length);
        _observed.ShouldContainOnly(acknowledged.Select(append => append.Id));
        _observed.Count.ShouldEqual(acknowledged.Length);
        acknowledged.Select(append => append.Sequence).Distinct().Count().ShouldEqual(acknowledged.Length);
        acknowledged.Select(append => append.Sequence).SequenceEqual(acknowledged.Select(append => append.Sequence).Order()).ShouldBeTrue();
        acknowledged.Zip(acknowledged.Skip(1), (first, next) => next.Sequence.Value == first.Sequence.Value + 1).All(contiguous => contiguous).ShouldBeTrue();
        stored.Select(@event => @event.Context.SequenceNumber).ShouldContainOnly(acknowledged.Select(append => append.Sequence));
    }

    public async ValueTask DisposeAsync()
    {
        await _stop.CancelAsync();
        try
        {
            await _load;
        }
        finally
        {
            _client?.Dispose();
            _stop.Dispose();
        }
    }

    async Task Run()
    {
        // Offered load is deliberately paced, not a readiness sleep. One attempt is never silently retried.
        using var cadence = new PeriodicTimer(TimeSpan.FromMilliseconds(100));
        while (!_stop.IsCancellationRequested)
        {
            var id = Guid.NewGuid();
            try
            {
                var result = await _store.EventLog.Append(_source, new ClusterEventAppended(id)).WaitAsync(TimeSpan.FromSeconds(30));
                if (result.IsSuccess)
                {
                    _acknowledged.Enqueue((id, result.SequenceNumber));
                }
                else
                {
                    Failed($"{id}: {string.Join(';', result.Errors)}");
                }
            }
            catch (RpcException exception)
            {
                Failed($"{id}: {exception.StatusCode}: {exception.Message}");
            }

            if (!_stop.IsCancellationRequested)
            {
                await cadence.WaitForNextTickAsync();
            }
        }
    }

    void Failed(string reason)
    {
        Console.WriteLine($"Failed append: {reason}");
        if (++_failures > 30)
        {
            throw new InvalidOperationException("More than 30 append attempts failed during the membership transition.");
        }
    }
}
