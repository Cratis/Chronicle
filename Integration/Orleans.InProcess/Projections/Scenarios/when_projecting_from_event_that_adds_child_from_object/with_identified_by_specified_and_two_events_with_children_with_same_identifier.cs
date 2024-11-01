// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Base;
using Cratis.Chronicle.Integration.Orleans.InProcess.Projections.Events;
using Cratis.Chronicle.Integration.Orleans.InProcess.Projections.Models;
using context = Cratis.Chronicle.Integration.Orleans.InProcess.Projections.Scenarios.when_projecting_from_event_that_adds_child_from_object.with_identified_by_specified_and_two_events_with_children_with_same_identifier.context;

namespace Cratis.Chronicle.Integration.Orleans.InProcess.Projections.Scenarios.when_projecting_from_event_that_adds_child_from_object;

[Collection(GlobalCollection.Name)]
public class with_identified_by_specified_and_two_events_with_children_with_same_identifier(context context) : Given<context>(context)
{
    public class context(GlobalFixture globalFixture) : given.a_projection_and_events_appended_to_it<IdentifiableChildrenProjection, ModelWithChildren>(globalFixture)
    {
        string model_id;
        public EventWithChildObject FirstEventAppended;
        public EventWithChildObject SecondEventAppended;

        public Model child;

        public override IEnumerable<Type> EventTypes => [typeof(EventWithChildObject)];


        void Establish()
        {
            model_id = Guid.NewGuid().ToString();

            FirstEventAppended = EventWithChildObject.Create();
            FirstEventAppended = FirstEventAppended with
            {
                Child = FirstEventAppended.Child with
                {
                    StringValue = "TheKey"
                }
            };
            EventsToAppend.Add(FirstEventAppended);

            SecondEventAppended = EventWithChildObject.Create();
            SecondEventAppended = SecondEventAppended with
            {
                Child = SecondEventAppended.Child with
                {
                    StringValue = "TheKey"
                }
            };
            EventsToAppend.Add(SecondEventAppended);
        }

        void Because()
        {
            child = Result.Children.ToArray()[0];
        }
    }

    [Fact] void should_have_one_child() => Context.Result.Children.Count().ShouldEqual(1);
    [Fact] void should_set_the_string_value_for_second_child() => Context.child.StringValue.ShouldEqual(Context.SecondEventAppended.Child.StringValue);
    [Fact] void should_set_the_bool_value_for_second_child() => Context.child.BoolValue.ShouldEqual(Context.SecondEventAppended.Child.BoolValue);
    [Fact] void should_set_the_int_value_for_second_child() => Context.child.IntValue.ShouldEqual(Context.SecondEventAppended.Child.IntValue);
    [Fact] void should_set_the_float_value_for_second_child() => Context.child.FloatValue.ShouldEqual(Context.SecondEventAppended.Child.FloatValue);
    [Fact] void should_set_the_double_value_for_second_child() => Context.child.DoubleValue.ShouldEqual(Context.SecondEventAppended.Child.DoubleValue);
    [Fact] void should_set_the_enum_value_for_second_child() => Context.child.EnumValue.ShouldEqual(Context.SecondEventAppended.Child.EnumValue);
    [Fact] void should_set_the_guid_value_for_second_child() => Context.child.GuidValue.ShouldEqual(Context.SecondEventAppended.Child.GuidValue);
    [Fact] void should_set_the_date_time_value_for_second_child() => Context.child.DateTimeValue.ShouldEqual(Context.SecondEventAppended.Child.DateTimeValue);
    [Fact] void should_set_the_date_only_value_for_second_child() => Context.child.DateOnlyValue.ShouldEqual(Context.SecondEventAppended.Child.DateOnlyValue);
    [Fact] void should_set_the_time_only_value_for_second_child() => Context.child.TimeOnlyValue.ShouldEqual(Context.SecondEventAppended.Child.TimeOnlyValue);
    [Fact] void should_set_the_date_time_offset_value_for_second_child() => Context.child.DateTimeOffsetValue.ShouldEqual(Context.SecondEventAppended.Child.DateTimeOffsetValue);
    [Fact] void should_set_the_string_concept_value_for_second_child() => Context.child.StringConceptValue.ShouldEqual(Context.SecondEventAppended.Child.StringConceptValue);
    [Fact] void should_set_the_bool_concept_value_for_second_child() => Context.child.BoolConceptValue.ShouldEqual(Context.SecondEventAppended.Child.BoolConceptValue);
    [Fact] void should_set_the_int_concept_value_for_second_child() => Context.child.IntConceptValue.ShouldEqual(Context.SecondEventAppended.Child.IntConceptValue);
    [Fact] void should_set_the_float_concept_value_for_second_child() => Context.child.FloatConceptValue.ShouldEqual(Context.SecondEventAppended.Child.FloatConceptValue);
    [Fact] void should_set_the_double_concept_value_for_second_child() => Context.child.DoubleConceptValue.ShouldEqual(Context.SecondEventAppended.Child.DoubleConceptValue);
    [Fact] void should_set_the_guid_concept_value_for_second_child() => Context.child.GuidConceptValue.ShouldEqual(Context.SecondEventAppended.Child.GuidConceptValue);
    [Fact] void should_set_the_event_sequence_number_to_last_event() => Context.Result.__lastHandledEventSequenceNumber.ShouldEqual(Context.LastEventSequenceNumber);
}
