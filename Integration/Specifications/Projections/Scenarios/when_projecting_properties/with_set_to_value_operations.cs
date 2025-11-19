// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Specifications.Projections.Events;
using Cratis.Chronicle.Integration.Specifications.Projections.ProjectionTypes;
using Cratis.Chronicle.Integration.Specifications.Projections.ReadModels;
using context = Cratis.Chronicle.Integration.Specifications.Projections.Scenarios.when_projecting_properties.with_set_to_value_operations.context;

namespace Cratis.Chronicle.Integration.Specifications.Projections.Scenarios.when_projecting_properties;

[Collection(ChronicleCollection.Name)]
public class with_set_to_value_operations(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : given.a_projection_and_events_appended_to_it<SetValuesProjection, ReadModel>(chronicleFixture)
    {
        public override IEnumerable<Type> EventTypes => [typeof(EmptyEvent)];

        void Establish()
        {
            EventsToAppend.Add(new EmptyEvent());
        }
    }

    [Fact] void should_set_the_string_value() => Context.Result.StringValue.ShouldEqual(KnownValues.StringValue);
    [Fact] void should_set_the_bool_value() => Context.Result.BoolValue.ShouldEqual(KnownValues.BoolValue);
    [Fact] void should_set_the_int_value() => Context.Result.IntValue.ShouldEqual(KnownValues.IntValue);
    [Fact] void should_set_the_float_value() => Context.Result.FloatValue.ShouldEqual(KnownValues.FloatValue);
    [Fact] void should_set_the_double_value() => Context.Result.DoubleValue.ShouldEqual(KnownValues.DoubleValue);
    [Fact] void should_set_the_enum_value() => Context.Result.EnumValue.ShouldEqual(KnownValues.EnumValue);
    [Fact] void should_set_the_guid_value() => Context.Result.GuidValue.ShouldEqual(KnownValues.GuidValue);
    [Fact] void should_set_the_date_time_value() => Context.Result.DateTimeValue.ShouldEqual(KnownValues.DateTimeValue);
    [Fact] void should_set_the_date_only_value() => Context.Result.DateOnlyValue.ShouldEqual(KnownValues.DateOnlyValue);
    [Fact] void should_set_the_time_only_value() => Context.Result.TimeOnlyValue.ShouldEqual(KnownValues.TimeOnlyValue);
    [Fact] void should_set_the_date_time_offset_value() => Context.Result.DateTimeOffsetValue.ShouldEqual(KnownValues.DateTimeOffsetValue);
    [Fact] void should_set_the_string_concept_value() => Context.Result.StringConceptValue.ShouldEqual(KnownValues.StringConceptValue);
    [Fact] void should_set_the_bool_concept_value() => Context.Result.BoolConceptValue.ShouldEqual(KnownValues.BoolConceptValue);
    [Fact] void should_set_the_int_concept_value() => Context.Result.IntConceptValue.ShouldEqual(KnownValues.IntConceptValue);
    [Fact] void should_set_the_float_concept_value() => Context.Result.FloatConceptValue.ShouldEqual(KnownValues.FloatConceptValue);
    [Fact] void should_set_the_double_concept_value() => Context.Result.DoubleConceptValue.ShouldEqual(KnownValues.DoubleConceptValue);
    [Fact] void should_set_the_guid_concept_value() => Context.Result.GuidConceptValue.ShouldEqual(KnownValues.GuidConceptValue);
    [Fact] void should_set_the_event_sequence_number_to_last_event() => Context.Result.__lastHandledEventSequenceNumber.ShouldEqual(Context.LastEventSequenceNumber);
}
