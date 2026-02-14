// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Specifications.Projections.Events;
using Cratis.Chronicle.Integration.Specifications.Projections.ReadModels;
using context = Cratis.Chronicle.Integration.Specifications.Projections.Scenarios.when_projecting_from_event_that_adds_child_from_object.with_identified_by_specified_and_three_events_with_two_having_child_with_same_identifier.context;

namespace Cratis.Chronicle.Integration.Specifications.Projections.Scenarios.when_projecting_from_event_that_adds_child_from_object;

[Collection(ChronicleCollection.Name)]
public class with_identified_by_specified_and_three_events_with_two_having_child_with_same_identifier(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : given.a_projection_and_events_appended_to_it<IdentifiableChildrenProjection, ReadModelWithChildren>(chronicleFixture)
    {
        string _modelId;
        public EventWithChildObject FirstEventAppended;
        public EventWithChildObject SecondEventAppended;
        public EventWithChildObject ThirdEventAppended;

        public ReadModel FirstChild;
        public ReadModel SecondChild;

        public override IEnumerable<Type> EventTypes => [typeof(EventWithChildObject)];

        void Establish()
        {
            _modelId = Guid.NewGuid().ToString();

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
                    StringValue = "SecondKey"
                }
            };
            EventsToAppend.Add(SecondEventAppended);

            ThirdEventAppended = EventWithChildObject.Create();
            ThirdEventAppended = ThirdEventAppended with
            {
                Child = ThirdEventAppended.Child with
                {
                    StringValue = "SecondKey"
                }
            };
            EventsToAppend.Add(ThirdEventAppended);
        }

        void Because()
        {
            var children = Result.Children.ToArray();
            FirstChild = children[0];
            SecondChild = children[1];
        }
    }

    [Fact] void should_have_two_children() => Context.Result.Children.Count().ShouldEqual(2);

    [Fact] void should_set_the_string_value_for_first_child() => Context.FirstChild.StringValue.ShouldEqual(Context.FirstEventAppended.Child.StringValue);
    [Fact] void should_set_the_bool_value_for_first_child() => Context.FirstChild.BoolValue.ShouldEqual(Context.FirstEventAppended.Child.BoolValue);
    [Fact] void should_set_the_int_value_for_first_child() => Context.FirstChild.IntValue.ShouldEqual(Context.FirstEventAppended.Child.IntValue);
    [Fact] void should_set_the_float_value_for_first_child() => Context.FirstChild.FloatValue.ShouldEqual(Context.FirstEventAppended.Child.FloatValue);
    [Fact] void should_set_the_double_value_for_first_child() => Context.FirstChild.DoubleValue.ShouldEqual(Context.FirstEventAppended.Child.DoubleValue);
    [Fact] void should_set_the_enum_value_for_first_child() => Context.FirstChild.EnumValue.ShouldEqual(Context.FirstEventAppended.Child.EnumValue);
    [Fact] void should_set_the_guid_value_for_first_child() => Context.FirstChild.GuidValue.ShouldEqual(Context.FirstEventAppended.Child.GuidValue);
    [Fact] void should_set_the_date_time_value_for_first_child() => Context.FirstChild.DateTimeValue.ShouldEqual(Context.FirstEventAppended.Child.DateTimeValue);
    [Fact] void should_set_the_date_only_value_for_first_child() => Context.FirstChild.DateOnlyValue.ShouldEqual(Context.FirstEventAppended.Child.DateOnlyValue);
    [Fact] void should_set_the_time_only_value_for_first_child() => Context.FirstChild.TimeOnlyValue.ShouldEqual(Context.FirstEventAppended.Child.TimeOnlyValue);
    [Fact] void should_set_the_date_time_offset_value_for_first_child() => Context.FirstChild.DateTimeOffsetValue.ShouldEqual(Context.FirstEventAppended.Child.DateTimeOffsetValue);
    [Fact] void should_set_the_string_concept_value_for_first_child() => Context.FirstChild.StringConceptValue.ShouldEqual(Context.FirstEventAppended.Child.StringConceptValue);
    [Fact] void should_set_the_bool_concept_value_for_first_child() => Context.FirstChild.BoolConceptValue.ShouldEqual(Context.FirstEventAppended.Child.BoolConceptValue);
    [Fact] void should_set_the_int_concept_value_for_first_child() => Context.FirstChild.IntConceptValue.ShouldEqual(Context.FirstEventAppended.Child.IntConceptValue);
    [Fact] void should_set_the_float_concept_value_for_first_child() => Context.FirstChild.FloatConceptValue.ShouldEqual(Context.FirstEventAppended.Child.FloatConceptValue);
    [Fact] void should_set_the_double_concept_value_for_first_child() => Context.FirstChild.DoubleConceptValue.ShouldEqual(Context.FirstEventAppended.Child.DoubleConceptValue);
    [Fact] void should_set_the_guid_concept_value_for_first_child() => Context.FirstChild.GuidConceptValue.ShouldEqual(Context.FirstEventAppended.Child.GuidConceptValue);

    [Fact] void should_set_the_string_value_for_second_child() => Context.SecondChild.StringValue.ShouldEqual(Context.ThirdEventAppended.Child.StringValue);
    [Fact] void should_set_the_bool_value_for_second_child() => Context.SecondChild.BoolValue.ShouldEqual(Context.ThirdEventAppended.Child.BoolValue);
    [Fact] void should_set_the_int_value_for_second_child() => Context.SecondChild.IntValue.ShouldEqual(Context.ThirdEventAppended.Child.IntValue);
    [Fact] void should_set_the_float_value_for_second_child() => Context.SecondChild.FloatValue.ShouldEqual(Context.ThirdEventAppended.Child.FloatValue);
    [Fact] void should_set_the_double_value_for_second_child() => Context.SecondChild.DoubleValue.ShouldEqual(Context.ThirdEventAppended.Child.DoubleValue);
    [Fact] void should_set_the_enum_value_for_second_child() => Context.SecondChild.EnumValue.ShouldEqual(Context.ThirdEventAppended.Child.EnumValue);
    [Fact] void should_set_the_guid_value_for_second_child() => Context.SecondChild.GuidValue.ShouldEqual(Context.ThirdEventAppended.Child.GuidValue);
    [Fact] void should_set_the_date_time_value_for_second_child() => Context.SecondChild.DateTimeValue.ShouldEqual(Context.ThirdEventAppended.Child.DateTimeValue);
    [Fact] void should_set_the_date_only_value_for_second_child() => Context.SecondChild.DateOnlyValue.ShouldEqual(Context.ThirdEventAppended.Child.DateOnlyValue);
    [Fact] void should_set_the_time_only_value_for_second_child() => Context.SecondChild.TimeOnlyValue.ShouldEqual(Context.ThirdEventAppended.Child.TimeOnlyValue);
    [Fact] void should_set_the_date_time_offset_value_for_second_child() => Context.SecondChild.DateTimeOffsetValue.ShouldEqual(Context.ThirdEventAppended.Child.DateTimeOffsetValue);
    [Fact] void should_set_the_string_concept_value_for_second_child() => Context.SecondChild.StringConceptValue.ShouldEqual(Context.ThirdEventAppended.Child.StringConceptValue);
    [Fact] void should_set_the_bool_concept_value_for_second_child() => Context.SecondChild.BoolConceptValue.ShouldEqual(Context.ThirdEventAppended.Child.BoolConceptValue);
    [Fact] void should_set_the_int_concept_value_for_second_child() => Context.SecondChild.IntConceptValue.ShouldEqual(Context.ThirdEventAppended.Child.IntConceptValue);
    [Fact] void should_set_the_float_concept_value_for_second_child() => Context.SecondChild.FloatConceptValue.ShouldEqual(Context.ThirdEventAppended.Child.FloatConceptValue);
    [Fact] void should_set_the_double_concept_value_for_second_child() => Context.SecondChild.DoubleConceptValue.ShouldEqual(Context.ThirdEventAppended.Child.DoubleConceptValue);
    [Fact] void should_set_the_guid_concept_value_for_second_child() => Context.SecondChild.GuidConceptValue.ShouldEqual(Context.ThirdEventAppended.Child.GuidConceptValue);
    [Fact] void should_set_the_event_sequence_number_to_last_event() => Context.Result.__lastHandledEventSequenceNumber.ShouldEqual(Context.LastEventSequenceNumber);
}
