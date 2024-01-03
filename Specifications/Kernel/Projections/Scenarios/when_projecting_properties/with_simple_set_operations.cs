// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Projections.Scenarios.Projections;
using Aksio.Cratis.Specifications.Integration;

namespace Aksio.Cratis.Kernel.Projections.Scenarios.when_projecting_properties;

public class with_simple_set_operations : ProjectionSpecificationFor<Model>
{
    string model_id;
    EventWithPropertiesForAllSupportedTypes event_appended;
    ProjectionResult<Model> result;

    protected override IProjectionFor<Model> CreateProjection() => new SetPropertiesProjection();

    async Task Because()
    {
        model_id = Guid.NewGuid().ToString();
        event_appended = EventWithPropertiesForAllSupportedTypes.CreateWithKnownValues();
        await context.EventLog.Append(model_id, event_appended);
        result = await context.GetById(model_id);
    }

    [Fact] void should_set_the_string_value() => result.Model.StringValue.ShouldEqual(event_appended.StringValue);
    [Fact] void should_set_the_bool_value() => result.Model.BoolValue.ShouldEqual(event_appended.BoolValue);
    [Fact] void should_set_the_int_value() => result.Model.IntValue.ShouldEqual(event_appended.IntValue);
    [Fact] void should_set_the_float_value() => result.Model.FloatValue.ShouldEqual(event_appended.FloatValue);
    [Fact] void should_set_the_double_value() => result.Model.DoubleValue.ShouldEqual(event_appended.DoubleValue);
    [Fact] void should_set_the_enum_value() => result.Model.EnumValue.ShouldEqual(event_appended.EnumValue);
    [Fact] void should_set_the_guid_value() => result.Model.GuidValue.ShouldEqual(event_appended.GuidValue);
    [Fact] void should_set_the_date_time_value() => result.Model.DateTimeValue.ShouldEqual(event_appended.DateTimeValue);
    [Fact] void should_set_the_date_only_value() => result.Model.DateOnlyValue.ShouldEqual(event_appended.DateOnlyValue);
    [Fact] void should_set_the_time_only_value() => result.Model.TimeOnlyValue.ShouldEqual(event_appended.TimeOnlyValue);
    [Fact] void should_set_the_date_time_offset_value() => result.Model.DateTimeOffsetValue.ShouldEqual(event_appended.DateTimeOffsetValue);
    [Fact] void should_set_the_string_concept_value() => result.Model.StringConceptValue.ShouldEqual(event_appended.StringConceptValue);
    [Fact] void should_set_the_bool_concept_value() => result.Model.BoolConceptValue.ShouldEqual(event_appended.BoolConceptValue);
    [Fact] void should_set_the_int_concept_value() => result.Model.IntConceptValue.ShouldEqual(event_appended.IntConceptValue);
    [Fact] void should_set_the_float_concept_value() => result.Model.FloatConceptValue.ShouldEqual(event_appended.FloatConceptValue);
    [Fact] void should_set_the_double_concept_value() => result.Model.DoubleConceptValue.ShouldEqual(event_appended.DoubleConceptValue);
    [Fact] void should_set_the_enum_concept_value() => result.Model.EnumConceptValue.ShouldEqual(event_appended.EnumConceptValue);
    [Fact] void should_set_the_guid_concept_value() => result.Model.GuidConceptValue.ShouldEqual(event_appended.GuidConceptValue);
    [Fact] void should_set_the_date_time_concept_value() => result.Model.DateTimeConceptValue.ShouldEqual(event_appended.DateTimeConceptValue);
    [Fact] void should_set_the_date_only_concept_value() => result.Model.DateOnlyConceptValue.ShouldEqual(event_appended.DateOnlyConceptValue);
    [Fact] void should_set_the_time_only_concept_value() => result.Model.TimeOnlyConceptValue.ShouldEqual(event_appended.TimeOnlyConceptValue);
    [Fact] void should_set_the_date_time_offset_concept_value() => result.Model.DateTimeOffsetConceptValue.ShouldEqual(event_appended.DateTimeOffsetConceptValue);
}
