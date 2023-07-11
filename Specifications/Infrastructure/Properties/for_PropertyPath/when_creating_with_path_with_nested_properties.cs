// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Strings;

namespace Aksio.Cratis.Properties.for_PropertyPath;

public class when_creating_with_path_with_nested_properties : Specification
{
    const string first_segment = "FirstSegment";
    const string second_segment = "SecondSegment";

    PropertyPath result;

    void Because() => result = new PropertyPath($"{first_segment}.{second_segment}");

    [Fact] void should_have_two_segments() => result.Segments.Count().ShouldEqual(2);
    [Fact] void should_have_first_level_be_property_name() => result.Segments.First().ShouldBeOfExactType<PropertyName>();
    [Fact] void should_have_first_level_value_be_lower_cased_property_name() => result.Segments.First().Value.ShouldEqual(first_segment.ToCamelCase());
    [Fact] void should_have_second_level_be_property_name() => result.Segments.ToArray()[1].ShouldBeOfExactType<PropertyName>();
    [Fact] void should_have_second_level_value_be_lower_cased_property_name() => result.Segments.ToArray()[1].Value.ShouldEqual(second_segment.ToCamelCase());
}
