// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using given = Cratis.Chronicle.Integration.Specifications.Projections.Scenarios.given;
using Cratis.Chronicle.Integration.Specifications.Projections.Events;
using Cratis.Chronicle.Integration.Specifications.Projections.ProjectionTypes;
using Cratis.Chronicle.Integration.Specifications.Projections.ReadModels;
using context = Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_functions.decrementing_per_key.context;

namespace Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_functions;

[Collection(ChronicleCollection.Name)]
public class decrementing_per_key(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture chronicleInProcessFixture) : given.a_projection_and_events_appended_to_it<DecrementingProjection, ReadModel>(chronicleInProcessFixture)
    {
        public EventSourceId SecondEventSourceId;
        public ReadModel SecondResult;

        public override IEnumerable<Type> EventTypes => [typeof(EventWithPropertiesForAllSupportedTypes)];

        void Establish()
        {
            SecondEventSourceId = Guid.NewGuid().ToString();
            EventsToAppend.Add(EventWithPropertiesForAllSupportedTypes.CreateWithRandomValues());
            EventsToAppend.Add(EventWithPropertiesForAllSupportedTypes.CreateWithRandomValues());
            EventsToAppend.Add(EventWithPropertiesForAllSupportedTypes.CreateWithRandomValues());
            EventsWithEventSourceIdToAppend.Add(new(SecondEventSourceId, EventWithPropertiesForAllSupportedTypes.CreateWithRandomValues()));
            EventsWithEventSourceIdToAppend.Add(new(SecondEventSourceId, EventWithPropertiesForAllSupportedTypes.CreateWithRandomValues()));
        }

        async Task Because() => SecondResult = await GetReadModel(SecondEventSourceId);
    }

    [Fact] void should_decrement_correctly_for_first_key() => Context.Result.IntValue.ShouldEqual(-3);
    [Fact] void should_decrement_correctly_for_second_key() => Context.SecondResult.IntValue.ShouldEqual(-2);
}
