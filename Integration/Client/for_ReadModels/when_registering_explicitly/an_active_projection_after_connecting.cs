// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using context = Cratis.Chronicle.Integration.for_ReadModels.when_registering_explicitly.an_active_projection_after_connecting.context;

namespace Cratis.Chronicle.Integration.for_ReadModels.when_registering_explicitly;

[Collection(ChronicleCollection.Name)]
public class an_active_projection_after_connecting(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : Specification<ChronicleFixture>(chronicleFixture)
    {
        public EventSourceId EventSourceId = $"explicit-{Guid.NewGuid()}";
        public ExplicitlyRegisteredReadModel Result;

        public override IEnumerable<Type> EventTypes => [typeof(SomeEvent)];

        async Task Because()
        {
            var handler = await EventStore.Projections.Register<ExplicitlyRegisteredReadModel>(projection => projection.From<SomeEvent>());
            await handler.WaitTillActive();

            var appendResult = await EventStore.EventLog.Append(EventSourceId, new SomeEvent(42));
            await handler.WaitTillReachesEventSequenceNumber(appendResult.SequenceNumber);

            Result = await EventStore.ReadModels.GetInstanceById<ExplicitlyRegisteredReadModel>(EventSourceId.Value);
        }
    }

    [Fact] void should_return_the_instance() => Context.Result.ShouldNotBeNull();
    [Fact] void should_project_the_event() => Context.Result.Number.ShouldEqual(42);
}
