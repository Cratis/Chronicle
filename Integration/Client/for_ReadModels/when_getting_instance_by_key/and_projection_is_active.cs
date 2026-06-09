// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using context = Cratis.Chronicle.Integration.for_ReadModels.when_getting_instance_by_key.and_projection_is_active.context;

namespace Cratis.Chronicle.Integration.for_ReadModels.when_getting_instance_by_key;

[Collection(ChronicleCollection.Name)]
public class and_projection_is_active(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : given.a_projection_with_events(chronicleFixture)
    {
        public SomeReadModel Result;

        async Task Because()
        {
            var handler = EventStore.Projections.GetHandlerFor<SomeProjection>();
            await handler.WaitTillActive();

            await EventStore.EventLog.Append(EventSourceId, FirstEvent);
            var appendResult = await EventStore.EventLog.Append(EventSourceId, SecondEvent);
            await handler.WaitTillReachesEventSequenceNumber(appendResult.SequenceNumber);

            // A materialized (active) projection resolves by its specific key from the stored sink.
            Result = await EventStore.ReadModels.GetInstanceById<SomeReadModel>(EventSourceId.Value);
        }
    }

    [Fact] void should_return_the_model() => Context.Result.ShouldNotBeNull();
    [Fact] void should_have_number_from_first_event() => Context.Result.Number.ShouldEqual(Context.FirstEvent.Number);
    [Fact] void should_have_value_from_second_event() => Context.Result.Value.ShouldEqual(Context.SecondEvent.Value);
}
