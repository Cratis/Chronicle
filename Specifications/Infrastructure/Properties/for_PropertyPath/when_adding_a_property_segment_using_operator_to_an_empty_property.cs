// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Properties.for_PropertyPath;

public class when_adding_a_property_segment_using_operator_to_an_empty_property : Specification
{
    const string left = "";
    const string right = "something";

    PropertyPath result;

    void Because() => result = new PropertyPath(left) + new PropertyName(right);

    [Fact] void should_hold_only_property_added_on() => result.Path.ShouldEqual(right);
}
