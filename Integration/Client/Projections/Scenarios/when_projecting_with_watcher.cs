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
                _tcs.SetResult();
            });

            Projection = EventStore.Projections.GetHandlerFor<AutoMappedPropertiesProjection>();
            await Projection.WaitTillSubscribed();

            // Allow the Watch's Orleans stream subscription to be established before appending.
            // WaitTillSubscribed ensures the projection event stream is ready, but the Watch
            // observable stream subscription (Watch<ReadModel>) is a separate Orleans stream
            // consumer that activates asynchronously. The outofprocess path involves an
            // additional network hop and Orleans stream storage write; after a MongoDB database
            // drop the stream consumer grain re-activates and must write its state before the
            // subscription is durable — 10 seconds gives enough margin on loaded CI runners.
            await Task.Delay(TimeSpanFactory.FromSeconds(10));

            var appendResult = await EventStore.EventLog.Append(EventSourceId, EventAppended);
            await Projection.WaitTillReachesEventSequenceNumber(appendResult.SequenceNumber);
            Result = await GetReadModel(EventSourceId);
        }

        async Task Because()
        {
            await _tcs.Task.WaitAsync(TimeSpanFactory.DefaultTimeout());
            _observable.Dispose();
        }
    }

    [Fact] void should_receive_same_model() => Context.Result.ShouldEqual(Context.WatchResult.ReadModel);
}
