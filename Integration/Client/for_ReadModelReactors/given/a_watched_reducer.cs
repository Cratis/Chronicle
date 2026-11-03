// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Integration.for_ReadModelReactors.given;

public class a_watched_reducer(ChronicleFixture fixture) : Specification(fixture)
{
    readonly CancellationTokenSource _lifetime = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
    public readonly WatchedReducer.State Control = new();
    public readonly RecordingReducerReadModelReactor Recorder = new();
    public Collector Watch;
    public Collector Reactions;
    public CancellationToken Token => _lifetime.Token;
    public IReducerHandler Handler => EventStore.Reducers.GetHandlerFor<WatchedReducer>();
    public override IEnumerable<Type> EventTypes => [typeof(ReducerWatchedEvent), typeof(ReducerWatchedDelete), typeof(ReducerWatchedNoChange), typeof(ReducerWatchedFailure)];
    public override IEnumerable<Type> Reducers => [typeof(WatchedReducer)];
    public override IEnumerable<Type> ReadModelReactors => [typeof(RecordingReducerReadModelReactor)];
    protected virtual bool SubscribeInitially => true;

    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(Control);
        services.AddTransient<WatchedReducer>();
        services.AddSingleton(Recorder);
    }

    async Task Establish()
    {
        _lifetime.CancelAfter(TimeSpan.FromSeconds(90));
        await Handler.WaitTillSubscribed();
        if (SubscribeInitially)
        {
            await StartWatching();
        }
    }

    public async Task StartWatching()
    {
        Watch?.Dispose();
        Reactions?.Dispose();
        Watch = new(EventStore.ReadModels.Watch<ReducerWatchedReadModel>());
        Reactions = new(Recorder.Changes);
        await Handshake();
    }

    public async Task Handshake()
    {
        var generation = Guid.NewGuid().ToString();
        var key = $"sentinel:{generation}";
        using var watch = new Collector(EventStore.ReadModels.Watch<ReducerWatchedReadModel>());
        using var reactions = new Collector(Recorder.Changes);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(Token);
        timeout.CancelAfter(TimeSpan.FromSeconds(30));
        var observed = Task.WhenAll(watch.WaitForSentinel(key, generation, timeout.Token), reactions.WaitForSentinel(key, generation, timeout.Token));
        await Task.WhenAll(observed, ProduceSentinel(key, generation, observed, timeout.Token));
    }

    async Task ProduceSentinel(string key, string generation, Task observed, CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await EventStore.EventLog.Append(key, new ReducerWatchedEvent(attempt, $"sentinel:{generation}:{attempt}")).WaitAsync(cancellationToken);
            try
            {
                await observed.WaitAsync(TimeSpan.FromMilliseconds(500), cancellationToken);
                return;
            }
            catch (TimeoutException)
            {
            }
        }
    }

    public async Task<ReadModelChangeset<ReducerWatchedReadModel>> Change(string key, int value, string marker)
    {
        var result = await EventStore.EventLog.Append(key, new ReducerWatchedEvent(value, marker));
        var frame = await Watch.WaitForSequence(key, result.SequenceNumber, Token);
        await Reactions.WaitForSequence(key, result.SequenceNumber, Token);
        return frame;
    }

    void Destroy() => Dispose();

    public override void Dispose()
    {
        _lifetime.Cancel();
        Control.Recover.TrySetResult();
        Watch?.Dispose();
        Reactions?.Dispose();
        Recorder.Close();
        _lifetime.Dispose();
        base.Dispose();
    }

    public sealed class Collector : IDisposable
    {
        readonly ConcurrentQueue<ReadModelChangeset<ReducerWatchedReadModel>> _frames = new();
        readonly ReplaySubject<ReadModelChangeset<ReducerWatchedReadModel>> _received = new();
        readonly IDisposable _subscription;
        readonly object _sync = new();

        public Collector(IObservable<ReadModelChangeset<ReducerWatchedReadModel>> observable)
        {
            _subscription = observable.Subscribe(frame =>
            {
                lock (_sync)
                {
                    _frames.Enqueue(frame);
                    _received.OnNext(frame);
                }
            });
        }

        public ReadModelChangeset<ReducerWatchedReadModel>[] For(string key) => _frames.Where(_ => _.ModelKey.Value == key && !key.StartsWith("sentinel:", StringComparison.Ordinal) && !(_.ReadModel?.Marker.StartsWith("sentinel:", StringComparison.Ordinal) ?? false)).ToArray();

        public Task<ReadModelChangeset<ReducerWatchedReadModel>> WaitForSequence(string key, EventSequenceNumber sequence, CancellationToken token) =>
            _received.Where(_ => _.ModelKey.Value == key && _.ChangeContext!.SequenceNumber == sequence).Take(1).ToTask(token);

        public Task<ReadModelChangeset<ReducerWatchedReadModel>> WaitForSentinel(string key, string generation, CancellationToken token) =>
            _received.Where(_ => _.ModelKey.Value == key && (_.ChangeType is ReadModelChangeType.Added or ReadModelChangeType.Modified) && (_.ReadModel?.Marker.StartsWith($"sentinel:{generation}:", StringComparison.Ordinal) ?? false)).Take(1).ToTask(token);

        public void Dispose()
        {
            _subscription.Dispose();
            _received.Dispose();
        }
    }
}
