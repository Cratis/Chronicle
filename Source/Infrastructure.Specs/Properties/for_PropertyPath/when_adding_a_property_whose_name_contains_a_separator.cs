// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Properties.for_PropertyPath;

public class when_adding_a_property_whose_name_contains_a_separator : Specification
{
    const string Left = "left";
    const string Right = "first.second";

    PropertyPath _result;

    void Because() => _result = new PropertyPath(Left).AddProperty(Right);

    [Fact] void should_combine_with_dot_separator() => _result.Path.ShouldEqual($"{Left}.{Right}");
    [Fact] void should_expand_the_name_into_one_segment_per_part() => _result.Segments.Count().ShouldEqual(3);
}
