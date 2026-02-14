// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Specifications.Projections.Events;
using Cratis.Chronicle.Integration.Specifications.Projections.ProjectionTypes;
using Cratis.Chronicle.Integration.Specifications.Projections.ReadModels;
using context = Cratis.Chronicle.Integration.Specifications.Projections.Scenarios.when_projecting_properties.with_auto_mapping.context;

namespace Cratis.Chronicle.Integration.Specifications.Projections.Scenarios.when_projecting_properties;

[Collection(ChronicleCollection.Name)]
public class with_auto_mapping(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : given.a_projection_and_events_appended_to_it<AutoMappedPropertiesProjection, ReadModel>(chronicleFixture)
    {
        public EventWithPropertiesForAllSupportedTypes EventAppended;

        public override IEnumerable<Type> EventTypes => [typeof(EventWithPropertiesForAllSupportedTypes)];

        void Establish()
        {
            EventAppended = EventWithPropertiesForAllSupportedTypes.CreateWithRandomValues();
            EventsToAppend.Add(EventAppended);
        }
    }

    [Fact] void should_set_the_string_value() => Context.Result.StringValue.ShouldEqual(Context.EventAppended.StringValue);
    [Fact] void should_set_the_bool_value() => Context.Result.BoolValue.ShouldEqual(Context.EventAppended.BoolValue);
    [Fact] void should_set_the_int_value() => Context.Result.IntValue.ShouldEqual(Context.EventAppended.IntValue);
    [Fact] void should_set_the_float_value() => Context.Result.FloatValue.ShouldEqual(Context.EventAppended.FloatValue);
    [Fact] void should_set_the_double_value() => Context.Result.DoubleValue.ShouldEqual(Context.EventAppended.DoubleValue);
    [Fact] void should_set_the_enum_value() => Context.Result.EnumValue.ShouldEqual(Context.EventAppended.EnumValue);
    [Fact] void should_set_the_guid_value() => Context.Result.GuidValue.ShouldEqual(Context.EventAppended.GuidValue);
    [Fact] void should_set_the_date_time_value() => Context.Result.DateTimeValue.ShouldEqual(Context.EventAppended.DateTimeValue);
    [Fact] void should_set_the_date_only_value() => Context.Result.DateOnlyValue.ShouldEqual(Context.EventAppended.DateOnlyValue);
    [Fact] void should_set_the_time_only_value() => Context.Result.TimeOnlyValue.ShouldEqual(Context.EventAppended.TimeOnlyValue);
    [Fact] void should_set_the_date_time_offset_value() => Context.Result.DateTimeOffsetValue.ShouldEqual(Context.EventAppended.DateTimeOffsetValue);
    [Fact] void should_set_the_string_concept_value() => Context.Result.StringConceptValue.ShouldEqual(Context.EventAppended.StringConceptValue);
    [Fact] void should_set_the_bool_concept_value() => Context.Result.BoolConceptValue.ShouldEqual(Context.EventAppended.BoolConceptValue);
    [Fact] void should_set_the_int_concept_value() => Context.Result.IntConceptValue.ShouldEqual(Context.EventAppended.IntConceptValue);
    [Fact] void should_set_the_float_concept_value() => Context.Result.FloatConceptValue.ShouldEqual(Context.EventAppended.FloatConceptValue);
    [Fact] void should_set_the_double_concept_value() => Context.Result.DoubleConceptValue.ShouldEqual(Context.EventAppended.DoubleConceptValue);
    [Fact] void should_set_the_guid_concept_value() => Context.Result.GuidConceptValue.ShouldEqual(Context.EventAppended.GuidConceptValue);
    [Fact] void should_set_the_event_sequence_number_to_last_event() => Context.Result.__lastHandledEventSequenceNumber.ShouldEqual(Context.LastEventSequenceNumber);
}
