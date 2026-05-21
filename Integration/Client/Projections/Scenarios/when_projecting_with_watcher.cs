// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
            _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
            EventAppended = EventWithPropertiesForAllSupportedTypes.CreateWithRandomValues();
            var watcher = EventStore.ReadModels.GetWatcherFor<ReadModel>();
            _observable = watcher.Observable.Subscribe(result =>
            {
                WatchResult = result;
                _tcs.TrySetResult();
            });

            Projection = EventStore.Projections.GetHandlerFor<AutoMappedPropertiesProjection>();
            await Projection.WaitTillSubscribed();

            // watcher.Subscribed only guarantees that the Watch handler's own subscription to the
            // ProjectionChangesets stream is registered. It does not guarantee that the projection's
            // observer subscriber grain has activated and loaded its definition; until both the
            // subscriber's pipeline is ready and Orleans pub-sub has propagated the new subscription,
            // an event appended immediately can be processed before any changeset reaches the watcher.
            // The short delay covers that window deterministically without depending on the full
            // multi-grain warmup path.
            await watcher.Subscribed.WaitAsync(TimeSpanFactory.DefaultTimeout());
            await Task.Delay(TimeSpanFactory.FromSeconds(2));
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
