// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Specifications.Integration;

namespace Aksio.Cratis.Kernel.Projections.Scenarios.when_projecting_from_event_that_adds_child_from_object;

public class with_identified_by_specified_and_two_events_with_children_with_same_identifier : ProjectionSpecificationFor<ModelWithChildren>
{
    string model_id;
    ProjectionResult<ModelWithChildren> result;
    protected override IProjectionFor<ModelWithChildren> CreateProjection() => new IdentifiableChildrenProjection();
    EventWithChildObject first_event_appended;
    EventWithChildObject second_event_appended;
    Model child;

    void Establish()
    {
        model_id = Guid.NewGuid().ToString();
        first_event_appended = EventWithChildObject.Create();
        first_event_appended = first_event_appended with
        {
            Child = first_event_appended.Child with
            {
                StringValue = "TheKey"
            }
        };

        second_event_appended = EventWithChildObject.Create();
        second_event_appended = second_event_appended with
        {
            Child = second_event_appended.Child with
            {
                StringValue = "TheKey"
            }
        };
    }

    async Task Because()
    {
        await context.EventLog.Append(model_id, first_event_appended);
        await context.EventLog.Append(model_id, second_event_appended);
        result = await context.GetById(model_id);

        child = result.Model.Children.ToArray()[0];
    }

    [Fact] void should_have_one_child() => result.Model.Children.Count().ShouldEqual(1);
    [Fact] void should_set_the_string_value_for_second_child() => child.StringValue.ShouldEqual(second_event_appended.Child.StringValue);
    [Fact] void should_set_the_bool_value_for_second_child() => child.BoolValue.ShouldEqual(second_event_appended.Child.BoolValue);
    [Fact] void should_set_the_int_value_for_second_child() => child.IntValue.ShouldEqual(second_event_appended.Child.IntValue);
    [Fact] void should_set_the_float_value_for_second_child() => child.FloatValue.ShouldEqual(second_event_appended.Child.FloatValue);
    [Fact] void should_set_the_double_value_for_second_child() => child.DoubleValue.ShouldEqual(second_event_appended.Child.DoubleValue);
    [Fact] void should_set_the_enum_value_for_second_child() => child.EnumValue.ShouldEqual(second_event_appended.Child.EnumValue);
    [Fact] void should_set_the_guid_value_for_second_child() => child.GuidValue.ShouldEqual(second_event_appended.Child.GuidValue);
    [Fact] void should_set_the_date_time_value_for_second_child() => child.DateTimeValue.ShouldEqual(second_event_appended.Child.DateTimeValue);
    [Fact] void should_set_the_date_only_value_for_second_child() => child.DateOnlyValue.ShouldEqual(second_event_appended.Child.DateOnlyValue);
    [Fact] void should_set_the_time_only_value_for_second_child() => child.TimeOnlyValue.ShouldEqual(second_event_appended.Child.TimeOnlyValue);
    [Fact] void should_set_the_date_time_offset_value_for_second_child() => child.DateTimeOffsetValue.ShouldEqual(second_event_appended.Child.DateTimeOffsetValue);
    [Fact] void should_set_the_string_concept_value_for_second_child() => child.StringConceptValue.ShouldEqual(second_event_appended.Child.StringConceptValue);
    [Fact] void should_set_the_bool_concept_value_for_second_child() => child.BoolConceptValue.ShouldEqual(second_event_appended.Child.BoolConceptValue);
    [Fact] void should_set_the_int_concept_value_for_second_child() => child.IntConceptValue.ShouldEqual(second_event_appended.Child.IntConceptValue);
    [Fact] void should_set_the_float_concept_value_for_second_child() => child.FloatConceptValue.ShouldEqual(second_event_appended.Child.FloatConceptValue);
    [Fact] void should_set_the_double_concept_value_for_second_child() => child.DoubleConceptValue.ShouldEqual(second_event_appended.Child.DoubleConceptValue);
    [Fact] void should_set_the_enum_concept_value_for_second_child() => child.EnumConceptValue.ShouldEqual(second_event_appended.Child.EnumConceptValue);
    [Fact] void should_set_the_guid_concept_value_for_second_child() => child.GuidConceptValue.ShouldEqual(second_event_appended.Child.GuidConceptValue);
    [Fact] void should_set_the_date_time_concept_value_for_second_child() => child.DateTimeConceptValue.ShouldEqual(second_event_appended.Child.DateTimeConceptValue);
    [Fact] void should_set_the_date_only_concept_value_for_second_child() => child.DateOnlyConceptValue.ShouldEqual(second_event_appended.Child.DateOnlyConceptValue);
    [Fact] void should_set_the_time_only_concept_value_for_second_child() => child.TimeOnlyConceptValue.ShouldEqual(second_event_appended.Child.TimeOnlyConceptValue);
    [Fact] void should_set_the_date_time_offset_concept_value_for_second_child() => child.DateTimeOffsetConceptValue.ShouldEqual(second_event_appended.Child.DateTimeOffsetConceptValue);
}
