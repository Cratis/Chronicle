// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Strings;

namespace Aksio.Cratis.Changes.for_ObjectsComparer
{
    public class when_comparing_object_with_top_level_changes : given.an_object_comparer
    {
        record TheType(string StringValue, int IntValue);

        TheType left;
        TheType right;

        bool result;

        IEnumerable<PropertyDifference<TheType>> differences;

        void Establish()
        {
            left = new TheType("FortyTwo", 42);
            right = new TheType("FortyThree", 43);
        }

        void Because() => result = comparer.Compare(left, right, out differences);

        // Notes:
        // - Handle Null values on either sides
        // - Handle Concept Types (Do not recurse into them - Value is not a property)
        // - Recurse
        // - Handle IDictionary<string, object> types
        // - Handle Guid types

        [Fact] void should_not_be_considered_equal() => result.ShouldBeTrue();
        [Fact] void should_have_first_difference_be_the_first_property() => differences.ToArray()[0].PropertyPath.Path.ShouldEqual(nameof(TheType.StringValue).ToCamelCase());
        [Fact] void should_have_second_difference_be_the_second_property() => differences.ToArray()[1].PropertyPath.Path.ShouldEqual(nameof(TheType.IntValue).ToCamelCase());
        [Fact] void should_hold_original_value_for_first_property() => differences.ToArray()[0].Original.ShouldEqual(left.StringValue);
        [Fact] void should_hold_changed_value_for_first_property() => differences.ToArray()[0].Changed.ShouldEqual(right.StringValue);
        [Fact] void should_hold_original_value_for_second_property() => differences.ToArray()[1].Original.ShouldEqual(left.IntValue);
        [Fact] void should_hold_changed_value_for_second_property() => differences.ToArray()[1].Changed.ShouldEqual(right.IntValue);
    }
}
