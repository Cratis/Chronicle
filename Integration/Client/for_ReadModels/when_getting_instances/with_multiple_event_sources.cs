// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using context = Cratis.Chronicle.Integration.for_ReadModels.when_getting_instances.with_multiple_event_sources.context;

namespace Cratis.Chronicle.Integration.for_ReadModels.when_getting_instances;

[Collection(ChronicleCollection.Name)]
public class with_multiple_event_sources(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : given.a_projection_with_events(chronicleFixture)
    {
        public EventSourceId SecondEventSourceId;
        public SomeEvent ThirdEvent;
        public AnotherEvent FourthEvent;
        public IEnumerable<SomeReadModel> Results;

        void Establish()
        {
            SecondEventSourceId = "another-source";
            ThirdEvent = new SomeEvent(100);
            FourthEvent = new AnotherEvent("another-value");
        }

        async Task Because()
        {
            await AppendEvents();
            await EventStore.EventLog.Append(SecondEventSourceId, ThirdEvent);
            await EventStore.EventLog.Append(SecondEventSourceId, FourthEvent);

            Results = await WaitTillInstancesAreVisible(2);
        }

        /// <summary>
        /// Gets the projected instance for the event source that appended <paramref name="number"/>.
        /// </summary>
        /// <param name="number">The number the event source's <see cref="SomeEvent"/> carried.</param>
        /// <returns>The instance belonging to that event source.</returns>
        /// <remarks>
        /// <c>GetInstances</c> reads the sink, and no sink promises an order — MongoDB answers in
        /// insertion order while SQL Server answers in clustered-key order, which puts
        /// <c>another-source</c> first. Identify each instance by what its event source appended
        /// rather than by where the backend happened to place it.
        /// </remarks>
        public SomeReadModel InstanceFor(int number) => Results.Single(_ => _.Number == number);
    }

    [Fact] void should_return_two_instances() => Context.Results.Count().ShouldEqual(2);
    [Fact] void should_have_an_instance_for_the_first_event_source() => Context.Results.Count(_ => _.Number == Context.FirstEvent.Number).ShouldEqual(1);
    [Fact] void should_have_the_first_event_sources_value() => Context.InstanceFor(Context.FirstEvent.Number).Value.ShouldEqual(Context.SecondEvent.Value);
    [Fact] void should_have_an_instance_for_the_second_event_source() => Context.Results.Count(_ => _.Number == Context.ThirdEvent.Number).ShouldEqual(1);
    [Fact] void should_have_the_second_event_sources_value() => Context.InstanceFor(Context.ThirdEvent.Number).Value.ShouldEqual(Context.FourthEvent.Value);
}
