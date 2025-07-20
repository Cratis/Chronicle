// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.InProcess.Integration.Projections.Events;
using Cratis.Chronicle.InProcess.Integration.Projections.Models;
using Cratis.Chronicle.InProcess.Integration.Projections.ProjectionTypes;
using context = Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_properties.adding_from_properties.context;

namespace Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_properties;

[Collection(ChronicleCollection.Name)]
public class adding_from_properties(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture chronicleInProcessFixture) : given.a_projection_and_events_appended_to_it<AddingFromPropertiesProjection, Model>(chronicleInProcessFixture)
    {
        public EventWithPropertiesForAllSupportedTypes FirstEventAppended;
        public EventWithPropertiesForAllSupportedTypes SecondEventAppended;

        public override IEnumerable<Type> EventTypes => [typeof(EventWithPropertiesForAllSupportedTypes)];

        void Establish()
        {
            FirstEventAppended = EventWithPropertiesForAllSupportedTypes.CreateWithRandomValues();
            EventsToAppend.Add(FirstEventAppended);
            SecondEventAppended = EventWithPropertiesForAllSupportedTypes.CreateWithRandomValues();
            EventsToAppend.Add(SecondEventAppended);
        }
    }

    [Fact] void should_result_in_correct_integer_value() => Context.Result.IntValue.ShouldEqual(Context.FirstEventAppended.IntValue + Context.SecondEventAppended.IntValue);
    [Fact] void should_result_in_correct_float_value() => Math.Round(Context.Result.FloatValue, 3).ShouldEqual(Math.Round(Context.FirstEventAppended.FloatValue + Context.SecondEventAppended.FloatValue, 3));
    [Fact] void should_result_in_correct_double_value() => Context.Result.DoubleValue.ShouldEqual(Context.FirstEventAppended.DoubleValue + Context.SecondEventAppended.DoubleValue);
    [Fact] void should_set_the_event_sequence_number_to_last_event() => Context.Result.__lastHandledEventSequenceNumber.ShouldEqual(Context.LastEventSequenceNumber);
}
