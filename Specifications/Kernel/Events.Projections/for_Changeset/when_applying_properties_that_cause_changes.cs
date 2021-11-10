// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Changes;
using Cratis.Properties;

namespace Cratis.Events.Projections
{
    public class when_applying_properties_that_cause_changes : Specification
    {
        Changeset<Event, ExpandoObject> changeset;
        ExpandoObject initial_state;
        Event @event;
        IEnumerable<PropertyMapper<Event, ExpandoObject>> property_mappers;
        Mock<IProjection> projection;

        void Establish()
        {
            initial_state = new();
            projection = new();

            ((dynamic)initial_state).Integer = 42;
            ((dynamic)initial_state).String = "Forty Two";
            dynamic nested = ((dynamic)initial_state).Nested = new ExpandoObject();
            nested.Integer = 43;
            nested.String = "Forty Three";

            property_mappers = new PropertyMapper<Event, ExpandoObject>[]
            {
                (_, target) => ((dynamic)target).Integer = 44,
                (_, target) => ((dynamic)target).String = "Forty Four",
                (_, target) => ((dynamic)target).Nested.Integer = 45,
                (_, target) => ((dynamic)target).Nested.String = "Forty Five",
            };

            @event = new Event(0, "some event", DateTimeOffset.UtcNow, string.Empty, new ExpandoObject());

            changeset = new(@event, initial_state);
        }

        void Because() => changeset.ApplyProperties(property_mappers);

        [Fact] void should_add_one_change_of_correct_type() => changeset.Changes.First().ShouldBeOfExactType<PropertiesChanged<ExpandoObject>>();
        [Fact] void should_add_a_property_diff_for_top_level_integer() => ((PropertiesChanged<ExpandoObject>)changeset.Changes.First()).Differences.ToArray()[0].MemberPath.ShouldEqual("Integer");
        [Fact] void should_add_a_property_diff_for_top_level_integers_value() => ((PropertiesChanged<ExpandoObject>)changeset.Changes.First()).Differences.ToArray()[0].Changed.ShouldEqual(44);
        [Fact] void should_add_a_property_diff_for_top_level_string() => ((PropertiesChanged<ExpandoObject>)changeset.Changes.First()).Differences.ToArray()[1].MemberPath.ShouldEqual("String");
        [Fact] void should_add_a_property_diff_for_top_level_strings_value() => ((PropertiesChanged<ExpandoObject>)changeset.Changes.First()).Differences.ToArray()[1].Changed.ShouldEqual("Forty Four");
        [Fact] void should_add_a_property_diff_for_nested_integer() => ((PropertiesChanged<ExpandoObject>)changeset.Changes.First()).Differences.ToArray()[2].MemberPath.ShouldEqual("Nested.Integer");
        [Fact] void should_add_a_property_diff_for_nested_integers_value() => ((PropertiesChanged<ExpandoObject>)changeset.Changes.First()).Differences.ToArray()[2].Changed.ShouldEqual(45);
        [Fact] void should_add_a_property_diff_for_nested_string() => ((PropertiesChanged<ExpandoObject>)changeset.Changes.First()).Differences.ToArray()[3].MemberPath.ShouldEqual("Nested.String");
        [Fact] void should_add_a_property_diff_for_nested_strings_value() => ((PropertiesChanged<ExpandoObject>)changeset.Changes.First()).Differences.ToArray()[3].Changed.ShouldEqual("Forty Five");
    }
}
