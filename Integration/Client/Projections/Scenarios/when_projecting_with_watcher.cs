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
            _observable = EventStore.ReadModels.Watch<ReadModel>().Subscribe(result =>
            {
                WatchResult = result;
                _tcs.TrySetResult();
            });

            Projection = EventStore.Projections.GetHandlerFor<AutoMappedPropertiesProjection>();
            await Projection.WaitTillSubscribed();

            // Allow the Watch's Orleans stream subscription to be established before appending.
            // WaitTillSubscribed ensures the projection event stream is ready, but the Watch
            // observable stream subscription (Watch<ReadModel>) is a separate Orleans stream
            // consumer that activates asynchronously. The delay gives the Watch grain time to
            // activate and register its change stream cursor before the event is appended so
            // the notification is not missed.
            await Task.Delay(TimeSpanFactory.FromSeconds(10));
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
