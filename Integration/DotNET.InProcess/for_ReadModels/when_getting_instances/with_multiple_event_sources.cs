// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using context = Cratis.Chronicle.InProcess.Integration.for_ReadModels.when_getting_instances.with_multiple_event_sources.context;

namespace Cratis.Chronicle.InProcess.Integration.for_ReadModels.when_getting_instances;

[Collection(ChronicleCollection.Name)]
public class with_multiple_event_sources(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture chronicleInProcessFixture) : given.a_projection_with_events(chronicleInProcessFixture)
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
            Results = await EventStore.ReadModels.GetInstances<SomeReadModel>();
        }
    }

    [Fact] void should_return_two_instances() => Context.Results.Count().ShouldEqual(2);
    [Fact] void should_have_first_instance_with_first_events() => Context.Results.First().Number.ShouldEqual(Context.FirstEvent.Number);
    [Fact] void should_have_first_instance_value() => Context.Results.First().Value.ShouldEqual(Context.SecondEvent.Value);
    [Fact] void should_have_second_instance_with_second_events() => Context.Results.Last().Number.ShouldEqual(Context.ThirdEvent.Number);
    [Fact] void should_have_second_instance_value() => Context.Results.Last().Value.ShouldEqual(Context.FourthEvent.Value);
}
