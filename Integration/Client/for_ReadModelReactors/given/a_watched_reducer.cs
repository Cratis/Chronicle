// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Integration.for_ReadModelReactors.given;

public class a_watched_reducer(ChronicleFixture fixture) : Specification<ChronicleFixture>(fixture)
{
    public override bool AutoDiscoverArtifacts => false;
    public override IEnumerable<Type> EventTypes => [typeof(Reduction)];
    public override IEnumerable<Type> Reducers => [typeof(WatchedReducer<ReducedModel>)];
    public override IEnumerable<Type> ReadModelReactors => [typeof(ReducerRecordingReactor<ReducedModel>)];
    public ReducerFrames WatchFrames { get; } = new();
    public ReducerFrames ReactorFrames { get; } = new();
    public WatchedReducer<ReducedModel> Reducer { get; } = new();
    protected IDisposable? Subscription { get; set; }
    protected IReducerHandler Handler => EventStore.Reducers.GetHandlerFor<WatchedReducer<ReducedModel>>();

    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(Reducer);
        services.AddSingleton(ReactorFrames);
        services.AddSingleton<ReducerRecordingReactor<ReducedModel>>();
    }

    protected void Subscribe() => Subscription = EventStore.ReadModels.Watch<ReducedModel>().Subscribe(change =>
        WatchFrames.Record(change.ChangeType, change.ChangeContext!));

    protected async Task Ready()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        await Handler.WaitTillSubscribed().WaitAsync(timeout.Token);
        await ReducerWatchReadiness.WaitFor(EventStore, WatchFrames, ReactorFrames, timeout.Token);
    }

    protected async Task<EventSequenceNumber> Append(string key, Reduction @event, ReadModelChangeType type, CancellationToken token)
    {
        var result = await EventStore.EventLog.Append(key, @event);
        await Received(key, type, result.SequenceNumber, token);
        return result.SequenceNumber;
    }

    protected async Task Received(string key, ReadModelChangeType type, EventSequenceNumber sequence, CancellationToken token)
    {
        await WatchFrames.WaitFor(key, type, sequence, token);
        await ReactorFrames.WaitFor(key, type, sequence, token);
    }

    void Destroy() => Subscription?.Dispose();
}
