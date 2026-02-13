// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.InProcess.Integration.Projections.Events;
using Cratis.Chronicle.InProcess.Integration.Projections.ProjectionTypes;
using Cratis.Chronicle.InProcess.Integration.Projections.ReadModels;
using Cratis.Chronicle.ReadModels;
using context = Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_watcher.context;

namespace Cratis.Chronicle.Integration.Specifications.Projections.Scenarios;

[Collection(ChronicleCollection.Name)]
public class when_projecting_with_watcher(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture chronicleInProcessFixture) : given.a_projection_and_events_appended_to_it<AutoMappedPropertiesProjection, ReadModel>(chronicleInProcessFixture)
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
            EventsToAppend.Add(EventAppended);
            _observable = EventStore.ReadModels.Watch<ReadModel>().Subscribe(result =>
            {
                WatchResult = result;
                _tcs.SetResult();
            });

            // Append the events after the watcher is ready
            Projection = EventStore.Projections.GetHandlerFor<AutoMappedPropertiesProjection>();
            await Projection.WaitTillActive();
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
