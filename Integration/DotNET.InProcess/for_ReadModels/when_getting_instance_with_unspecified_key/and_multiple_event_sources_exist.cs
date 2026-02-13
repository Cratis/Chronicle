// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using context = Cratis.Chronicle.InProcess.Integration.for_ReadModels.when_getting_instance_with_unspecified_key.and_multiple_event_sources_exist.context;

namespace Cratis.Chronicle.InProcess.Integration.for_ReadModels.when_getting_instance_with_unspecified_key;

[Collection(ChronicleCollection.Name)]
public class and_multiple_event_sources_exist(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture chronicleInProcessFixture) : given.a_projection_with_events(chronicleInProcessFixture)
    {
        public EventSourceId SecondEventSourceId;
        public SomeEvent ThirdEvent;
        public AnotherEvent FourthEvent;
        public SomeReadModel Result;

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
            Result = await EventStore.ReadModels.GetInstanceById<SomeReadModel>(ReadModelKey.Unspecified);
        }
    }

    [Fact] void should_have_number_from_last_event() => Context.Result.Number.ShouldEqual(Context.ThirdEvent.Number);
    [Fact] void should_have_value_from_last_event() => Context.Result.Value.ShouldEqual(Context.FourthEvent.Value);
}
