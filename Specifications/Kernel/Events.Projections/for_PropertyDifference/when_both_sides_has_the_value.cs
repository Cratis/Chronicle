// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using ObjectsComparer;

namespace Cratis.Events.Projections.for_PropertyDifference
{
    public class when_both_sides_has_the_value : Specification
    {
        PropertyDifference difference;

        ExpandoObject original;
        ExpandoObject changed;
        Difference raw_difference;

        void Establish()
        {
            dynamic theOriginal = original = new ExpandoObject();
            theOriginal.Integer = 42;
            dynamic theChanged = changed = new ExpandoObject();
            theChanged.Integer = 43;
            raw_difference = new Difference("Integer", "42", "43", DifferenceTypes.MissedMemberInSecondObject);
        }

        void Because() => difference = new PropertyDifference(original, changed, raw_difference);

        [Fact] void should_hold_original_value_as_correct_type() => difference.Original.ShouldEqual(42);
        [Fact] void should_hold_changed_value_as_correct_type() => difference.Changed.ShouldEqual(43);
    }
}
