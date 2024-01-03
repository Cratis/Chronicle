// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Projections.Scenarios.Projections;
using Aksio.Cratis.Specifications.Integration;

namespace Aksio.Cratis.Kernel.Projections.Scenarios.when_projecting_properties;

public class with_set_to_value_operations : ProjectionSpecificationFor<Model>
{
    string model_id;
    ProjectionResult<Model> result;
    protected override IProjectionFor<Model> CreateProjection() => new SetValuesProjection();

    async Task Because()
    {
        model_id = Guid.NewGuid().ToString();
        await context.EventLog.Append(model_id, new EmptyEvent());
        result = await context.GetById(model_id);
    }

    [Fact] void should_set_the_string_value() => result.Model.StringValue.ShouldEqual(KnownValues.StringValue);
    [Fact] void should_set_the_bool_value() => result.Model.BoolValue.ShouldEqual(KnownValues.BoolValue);
    [Fact] void should_set_the_int_value() => result.Model.IntValue.ShouldEqual(KnownValues.IntValue);
    [Fact] void should_set_the_float_value() => result.Model.FloatValue.ShouldEqual(KnownValues.FloatValue);
    [Fact] void should_set_the_double_value() => result.Model.DoubleValue.ShouldEqual(KnownValues.DoubleValue);
    [Fact] void should_set_the_enum_value() => result.Model.EnumValue.ShouldEqual(KnownValues.EnumValue);
    [Fact] void should_set_the_guid_value() => result.Model.GuidValue.ShouldEqual(KnownValues.GuidValue);
    [Fact] void should_set_the_date_time_value() => result.Model.DateTimeValue.ShouldEqual(KnownValues.DateTimeValue);
    [Fact] void should_set_the_date_only_value() => result.Model.DateOnlyValue.ShouldEqual(KnownValues.DateOnlyValue);
    [Fact] void should_set_the_time_only_value() => result.Model.TimeOnlyValue.ShouldEqual(KnownValues.TimeOnlyValue);
    [Fact] void should_set_the_date_time_offset_value() => result.Model.DateTimeOffsetValue.ShouldEqual(KnownValues.DateTimeOffsetValue);
    [Fact] void should_set_the_string_concept_value() => result.Model.StringConceptValue.ShouldEqual(KnownValues.StringConceptValue);
    [Fact] void should_set_the_bool_concept_value() => result.Model.BoolConceptValue.ShouldEqual(KnownValues.BoolConceptValue);
    [Fact] void should_set_the_int_concept_value() => result.Model.IntConceptValue.ShouldEqual(KnownValues.IntConceptValue);
    [Fact] void should_set_the_float_concept_value() => result.Model.FloatConceptValue.ShouldEqual(KnownValues.FloatConceptValue);
    [Fact] void should_set_the_double_concept_value() => result.Model.DoubleConceptValue.ShouldEqual(KnownValues.DoubleConceptValue);
    [Fact] void should_set_the_enum_concept_value() => result.Model.EnumConceptValue.ShouldEqual(KnownValues.EnumConceptValue);
    [Fact] void should_set_the_guid_concept_value() => result.Model.GuidConceptValue.ShouldEqual(KnownValues.GuidConceptValue);
    [Fact] void should_set_the_date_time_concept_value() => result.Model.DateTimeConceptValue.ShouldEqual(KnownValues.DateTimeConceptValue);
    [Fact] void should_set_the_date_only_concept_value() => result.Model.DateOnlyConceptValue.ShouldEqual(KnownValues.DateOnlyConceptValue);
    [Fact] void should_set_the_time_only_concept_value() => result.Model.TimeOnlyConceptValue.ShouldEqual(KnownValues.TimeOnlyConceptValue);
    [Fact] void should_set_the_date_time_offset_concept_value() => result.Model.DateTimeOffsetConceptValue.ShouldEqual(KnownValues.DateTimeOffsetConceptValue);
}
