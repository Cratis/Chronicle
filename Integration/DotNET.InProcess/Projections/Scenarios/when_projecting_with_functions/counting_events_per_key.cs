// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.InProcess.Integration.Projections.Events;
using Cratis.Chronicle.InProcess.Integration.Projections.ProjectionTypes;
using Cratis.Chronicle.InProcess.Integration.Projections.ReadModels;
using context = Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_functions.counting_events_per_key.context;

namespace Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_functions;

[Collection(ChronicleCollection.Name)]
public class counting_events_per_key(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture chronicleInProcessFixture) : given.a_projection_and_events_appended_to_it<CountingEventsProjection, ReadModel>(chronicleInProcessFixture)
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

        async Task Because()
        {
            Result ??= await WaitForReadModel(EventSourceId);
            SecondResult = await WaitForReadModel(SecondEventSourceId);
        }

        async Task<ReadModel> WaitForReadModel(EventSourceId eventSourceId)
        {
            var timeout = DateTime.UtcNow.Add(TimeSpanFactory.DefaultTimeout());
            while (DateTime.UtcNow < timeout)
            {
                var model = await GetReadModel(eventSourceId);
                if (model is not null)
                {
                    return model;
                }

                await Task.Delay(100);
            }

            throw new TimeoutException($"Read model for event source '{eventSourceId}' was not materialized within timeout.");
        }
    }

    [Fact] void should_hold_correct_count_for_first_key() => Context.Result.IntValue.ShouldEqual(3);
    [Fact] void should_hold_correct_count_for_second_key() => Context.SecondResult.IntValue.ShouldEqual(2);
}
