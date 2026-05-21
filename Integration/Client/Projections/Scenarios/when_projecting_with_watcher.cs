// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Integration.Projections.Events;
using Cratis.Chronicle.Integration.Projections.ProjectionTypes;
using Cratis.Chronicle.ReadModels;
using context = Cratis.Chronicle.Integration.Projections.Scenarios.when_projecting_with_watcher.context;
using ReadModel = Cratis.Chronicle.Integration.Projections.ReadModels.ReadModel;

namespace Cratis.Chronicle.Integration.Projections.Scenarios;

[Collection(ChronicleCollection.Name)]
public class when_projecting_with_watcher(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleInProcessFixture) : given.a_projection_and_events_appended_to_it<AutoMappedPropertiesProjection, ReadModel>(chronicleInProcessFixture)
    {
        public EventWithPropertiesForAllSupportedTypes EventAppended;

        public override IEnumerable<Type> EventTypes => [typeof(EventWithPropertiesForAllSupportedTypes)];

        public ReadModelChangeset<ReadModel> WatchResult;

        TaskCompletionSource _tcs;
#pragma warning disable CA2213 // Disposable fields should be disposed
        IDisposable _observable;
#pragma warning restore CA2213 // Disposable fields should be disposed

        async Task Establish()
        {
            EventAppended = EventWithPropertiesForAllSupportedTypes.CreateWithRandomValues();
            var watcher = EventStore.ReadModels.GetWatcherFor<ReadModel>();
            Projection = EventStore.Projections.GetHandlerFor<AutoMappedPropertiesProjection>();
            await Projection.WaitTillSubscribed();

            // The deterministic watcher.Subscribed signal covers the Watch handler's own server-side
            // subscription to the ProjectionChangesets stream, but the projection's observer
            // subscriber grain is activated lazily on first event and goes through OnActivateAsync,
            // definition load and pipeline construction before any changeset reaches the stream.
            // Drive it through that warmup explicitly here by appending and discarding a sentinel
            // event in a dedicated event source: once a changeset for the sentinel has been seen,
            // the projection grain is fully warm and any subsequent event will produce a changeset
            // without racing the first-event activation path.
            await watcher.Subscribed.WaitAsync(TimeSpanFactory.DefaultTimeout());

            var warmupSourceId = (EventSourceId)Guid.NewGuid().ToString();
            var warmupTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            using (watcher.Observable
                .Where(c => c.ModelKey == warmupSourceId.Value)
                .Subscribe(_ => warmupTcs.TrySetResult()))
            {
                await EventStore.EventLog.Append(warmupSourceId, EventWithPropertiesForAllSupportedTypes.CreateWithRandomValues());
                await warmupTcs.Task.WaitAsync(TimeSpanFactory.DefaultTimeout());
            }

            // Subscribe for the actual test event only after warmup is complete so that the
            // sentinel changeset does not satisfy the assertion that the watcher saw the
            // EventAppended event.
            _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
            _observable = watcher.Observable
                .Where(c => c.ModelKey == EventSourceId.Value)
                .Subscribe(result =>
                {
                    WatchResult = result;
                    _tcs.TrySetResult();
                });
        }

        async Task Because()
        {
            await EventStore.EventLog.Append(EventSourceId, EventAppended);
            await _tcs.Task.WaitAsync(TimeSpanFactory.DefaultTimeout());
            _observable.Dispose();
            Result = await GetReadModel(EventSourceId);
        }
    }

    [Fact] void should_receive_same_model() => Context.Result.ShouldEqual(Context.WatchResult.ReadModel);
}
