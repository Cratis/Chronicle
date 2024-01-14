// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Specifications.Integration;

namespace Aksio.Cratis.Kernel.Projections.Scenarios.when_projecting_from_event_that_adds_child_from_object;


public class without_identified_by_specified : ProjectionSpecificationFor<ModelWithChildren>
{
    string model_id;
    ProjectionResult<ModelWithChildren> result;
    protected override IProjectionFor<ModelWithChildren> CreateProjection() => new UnidentifiableChildrenProjection();
    EventWithChildObject first_event_appended;
    EventWithChildObject second_event_appended;
    Model first_child;
    Model second_child;

    void Establish()
    {
        model_id = Guid.NewGuid().ToString();
        first_event_appended = EventWithChildObject.Create();
        second_event_appended = EventWithChildObject.Create();
    }

    async Task Because()
    {
        await context.EventLog.Append(model_id, first_event_appended);
        await context.EventLog.Append(model_id, second_event_appended);
        result = await context.GetById(model_id);

        var children = result.Model.Children.ToArray();
        first_child = children[0];
        second_child = children[1];
    }

    [Fact] void should_have_two_children() => result.Model.Children.Count().ShouldEqual(2);
    [Fact] void should_set_the_string_value_for_first_child() => first_child.StringValue.ShouldEqual(first_event_appended.Child.StringValue);
    [Fact] void should_set_the_bool_value_for_first_child() => first_child.BoolValue.ShouldEqual(first_event_appended.Child.BoolValue);
    [Fact] void should_set_the_int_value_for_first_child() => first_child.IntValue.ShouldEqual(first_event_appended.Child.IntValue);
    [Fact] void should_set_the_float_value_for_first_child() => first_child.FloatValue.ShouldEqual(first_event_appended.Child.FloatValue);
    [Fact] void should_set_the_double_value_for_first_child() => first_child.DoubleValue.ShouldEqual(first_event_appended.Child.DoubleValue);
    [Fact] void should_set_the_enum_value_for_first_child() => first_child.EnumValue.ShouldEqual(first_event_appended.Child.EnumValue);
    [Fact] void should_set_the_guid_value_for_first_child() => first_child.GuidValue.ShouldEqual(first_event_appended.Child.GuidValue);
    [Fact] void should_set_the_date_time_value_for_first_child() => first_child.DateTimeValue.ShouldEqual(first_event_appended.Child.DateTimeValue);
    [Fact] void should_set_the_date_only_value_for_first_child() => first_child.DateOnlyValue.ShouldEqual(first_event_appended.Child.DateOnlyValue);
    [Fact] void should_set_the_time_only_value_for_first_child() => first_child.TimeOnlyValue.ShouldEqual(first_event_appended.Child.TimeOnlyValue);
    [Fact] void should_set_the_date_time_offset_value_for_first_child() => first_child.DateTimeOffsetValue.ShouldEqual(first_event_appended.Child.DateTimeOffsetValue);
    [Fact] void should_set_the_string_concept_value_for_first_child() => first_child.StringConceptValue.ShouldEqual(first_event_appended.Child.StringConceptValue);
    [Fact] void should_set_the_bool_concept_value_for_first_child() => first_child.BoolConceptValue.ShouldEqual(first_event_appended.Child.BoolConceptValue);
    [Fact] void should_set_the_int_concept_value_for_first_child() => first_child.IntConceptValue.ShouldEqual(first_event_appended.Child.IntConceptValue);
    [Fact] void should_set_the_float_concept_value_for_first_child() => first_child.FloatConceptValue.ShouldEqual(first_event_appended.Child.FloatConceptValue);
    [Fact] void should_set_the_double_concept_value_for_first_child() => first_child.DoubleConceptValue.ShouldEqual(first_event_appended.Child.DoubleConceptValue);
    [Fact] void should_set_the_enum_concept_value_for_first_child() => first_child.EnumConceptValue.ShouldEqual(first_event_appended.Child.EnumConceptValue);
    [Fact] void should_set_the_guid_concept_value_for_first_child() => first_child.GuidConceptValue.ShouldEqual(first_event_appended.Child.GuidConceptValue);
    [Fact] void should_set_the_date_time_concept_value_for_first_child() => first_child.DateTimeConceptValue.ShouldEqual(first_event_appended.Child.DateTimeConceptValue);
    [Fact] void should_set_the_date_only_concept_value_for_first_child() => first_child.DateOnlyConceptValue.ShouldEqual(first_event_appended.Child.DateOnlyConceptValue);
    [Fact] void should_set_the_time_only_concept_value_for_first_child() => first_child.TimeOnlyConceptValue.ShouldEqual(first_event_appended.Child.TimeOnlyConceptValue);
    [Fact] void should_set_the_date_time_offset_concept_value_for_first_child() => first_child.DateTimeOffsetConceptValue.ShouldEqual(first_event_appended.Child.DateTimeOffsetConceptValue);

    [Fact] void should_set_the_string_value_for_second_child() => second_child.StringValue.ShouldEqual(second_event_appended.Child.StringValue);
    [Fact] void should_set_the_bool_value_for_second_child() => second_child.BoolValue.ShouldEqual(second_event_appended.Child.BoolValue);
    [Fact] void should_set_the_int_value_for_second_child() => second_child.IntValue.ShouldEqual(second_event_appended.Child.IntValue);
    [Fact] void should_set_the_float_value_for_second_child() => second_child.FloatValue.ShouldEqual(second_event_appended.Child.FloatValue);
    [Fact] void should_set_the_double_value_for_second_child() => second_child.DoubleValue.ShouldEqual(second_event_appended.Child.DoubleValue);
    [Fact] void should_set_the_enum_value_for_second_child() => second_child.EnumValue.ShouldEqual(second_event_appended.Child.EnumValue);
    [Fact] void should_set_the_guid_value_for_second_child() => second_child.GuidValue.ShouldEqual(second_event_appended.Child.GuidValue);
    [Fact] void should_set_the_date_time_value_for_second_child() => second_child.DateTimeValue.ShouldEqual(second_event_appended.Child.DateTimeValue);
    [Fact] void should_set_the_date_only_value_for_second_child() => second_child.DateOnlyValue.ShouldEqual(second_event_appended.Child.DateOnlyValue);
    [Fact] void should_set_the_time_only_value_for_second_child() => second_child.TimeOnlyValue.ShouldEqual(second_event_appended.Child.TimeOnlyValue);
    [Fact] void should_set_the_date_time_offset_value_for_second_child() => second_child.DateTimeOffsetValue.ShouldEqual(second_event_appended.Child.DateTimeOffsetValue);
    [Fact] void should_set_the_string_concept_value_for_second_child() => second_child.StringConceptValue.ShouldEqual(second_event_appended.Child.StringConceptValue);
    [Fact] void should_set_the_bool_concept_value_for_second_child() => second_child.BoolConceptValue.ShouldEqual(second_event_appended.Child.BoolConceptValue);
    [Fact] void should_set_the_int_concept_value_for_second_child() => second_child.IntConceptValue.ShouldEqual(second_event_appended.Child.IntConceptValue);
    [Fact] void should_set_the_float_concept_value_for_second_child() => second_child.FloatConceptValue.ShouldEqual(second_event_appended.Child.FloatConceptValue);
    [Fact] void should_set_the_double_concept_value_for_second_child() => second_child.DoubleConceptValue.ShouldEqual(second_event_appended.Child.DoubleConceptValue);
    [Fact] void should_set_the_enum_concept_value_for_second_child() => second_child.EnumConceptValue.ShouldEqual(second_event_appended.Child.EnumConceptValue);
    [Fact] void should_set_the_guid_concept_value_for_second_child() => second_child.GuidConceptValue.ShouldEqual(second_event_appended.Child.GuidConceptValue);
    [Fact] void should_set_the_date_time_concept_value_for_second_child() => second_child.DateTimeConceptValue.ShouldEqual(second_event_appended.Child.DateTimeConceptValue);
    [Fact] void should_set_the_date_only_concept_value_for_second_child() => second_child.DateOnlyConceptValue.ShouldEqual(second_event_appended.Child.DateOnlyConceptValue);
    [Fact] void should_set_the_time_only_concept_value_for_second_child() => second_child.TimeOnlyConceptValue.ShouldEqual(second_event_appended.Child.TimeOnlyConceptValue);
    [Fact] void should_set_the_date_time_offset_concept_value_for_second_child() => second_child.DateTimeOffsetConceptValue.ShouldEqual(second_event_appended.Child.DateTimeOffsetConceptValue);
}
