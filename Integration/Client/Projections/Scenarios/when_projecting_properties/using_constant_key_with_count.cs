// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Projections.Events;
using Cratis.Chronicle.Integration.Projections.ProjectionTypes;
using Cratis.Chronicle.Integration.Projections.ReadModels;
using context = Cratis.Chronicle.Integration.Projections.Scenarios.when_projecting_properties.using_constant_key_with_count.context;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.when_projecting_properties;

[Collection(ChronicleCollection.Name)]
public class using_constant_key_with_count(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : given.a_projection_and_events_appended_to_it<CountWithConstantKeyProjection, CounterReadModel>(chronicleFixture)
    {
        public override IEnumerable<Type> EventTypes => [typeof(EmptyEvent)];

        void Establish()
        {
            ReadModelId = CountWithConstantKeyProjection.ConstantKeyValue;

            // Append events from three different event source IDs - they should all aggregate into one read model
            EventsWithEventSourceIdToAppend.Add(new(Guid.NewGuid().ToString(), new EmptyEvent()));
            EventsWithEventSourceIdToAppend.Add(new(Guid.NewGuid().ToString(), new EmptyEvent()));
            EventsWithEventSourceIdToAppend.Add(new(Guid.NewGuid().ToString(), new EmptyEvent()));
        }
    }

    [Fact] void should_return_single_read_model() => Context.Result.ShouldNotBeNull();
    [Fact] void should_count_all_events_regardless_of_event_source() => Context.Result.Count.ShouldEqual(3);
    [Fact] void should_set_the_event_sequence_number_to_last_event() => Context.Result.__lastHandledEventSequenceNumber.ShouldEqual(Context.LastEventSequenceNumber);
}
