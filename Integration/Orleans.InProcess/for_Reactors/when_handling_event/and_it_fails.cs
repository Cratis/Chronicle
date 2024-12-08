// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Grains.Observation;
using Cratis.Chronicle.Integration.Base;
using context = Cratis.Chronicle.Integration.Orleans.InProcess.for_Reactors.when_handling_event.and_it_fails.context;

namespace Cratis.Chronicle.Integration.Orleans.InProcess.for_Reactors.when_handling_event;

[Collection(GlobalCollection.Name)]
public class and_it_fails(context context) : Given<context>(context)
{
    public class context(GlobalFixture globalFixture) : given.a_reactor_observing_an_event(globalFixture)
    {
        public TaskCompletionSource Tcs;
        public IEnumerable<FailedPartition> FailedPartitions;
        public ReactorThatCanFail Observer;
        public IObserver ReactorObserver;
        public SomeEvent Event;
        public EventSourceId EventSourceId;

        public override IEnumerable<Type> EventTypes => [typeof(SomeEvent)];
        public override IEnumerable<Type> Reactors => [typeof(ReactorThatCanFail)];

        protected override void ConfigureServices(IServiceCollection services)
        {
            Tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
            Observer = new ReactorThatCanFail(Tcs);
            services.AddSingleton(Observer);
        }

        void Establish()
        {
            EventSourceId = "some source";
            Event = new SomeEvent(42);
            ReactorObserver = GetObserverFor<ReactorThatCanFail>();
        }

        async Task Because()
        {
            await ReactorObserver.WaitTillActive();
            Observer.ShouldFail = true;
            await EventStore.EventLog.Append(EventSourceId, Event);
            await Tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

            var reactorObserverState = await ReactorObserver.GetState();

            FailedPartitions = await EventStore.Connection.Services.Observers.GetFailedPartitionsForObserver(new()
            {
                EventStoreName = EventStore.Name.Value,
                Namespace = Concepts.EventStoreNamespaceName.Default,
                ObserverId = reactorObserverState.Id
            });
        }
    }

    [Fact] void should_have_one_failing_partition() => Context.FailedPartitions.Count().ShouldEqual(1);
}
