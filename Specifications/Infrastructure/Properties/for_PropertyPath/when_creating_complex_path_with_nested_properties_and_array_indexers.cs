// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Strings;

namespace Aksio.Cratis.Properties.for_PropertyPath;

public class when_creating_complex_path_with_nested_properties_and_array_indexers : Specification
{
    const string first_segment = "FirstSegment";
    const string second_segment = "SecondSegment";
    const string third_segment = "ThirdSegment";
    const string forth_segment = "ForthSegment";
    const string fifth_segment = "FifthSegment";
    const string sixth_segment = "SixthSegment";

    PropertyPath result;

    void Because() => result = new PropertyPath($"{first_segment}.[{second_segment}].{third_segment}.{forth_segment}.[{fifth_segment}].{sixth_segment}");

    [Fact] void should_have_six_segments() => result.Segments.Count().ShouldEqual(6);
    [Fact] void should_have_first_level_be_property_name() => result.Segments.First().ShouldBeOfExactType<PropertyName>();
    [Fact] void should_have_first_level_value_be_lower_cased_property_name() => result.Segments.First().Value.ShouldEqual(first_segment.ToCamelCase());
    [Fact] void should_have_second_level_be_array_index() => result.Segments.ToArray()[1].ShouldBeOfExactType<ArrayProperty>();
    [Fact] void should_have_second_level_value_be_lower_cased_property_name() => result.Segments.ToArray()[1].Value.ShouldEqual(second_segment.ToCamelCase());
    [Fact] void should_have_third_level_be_property_name() => result.Segments.ToArray()[2].ShouldBeOfExactType<PropertyName>();
    [Fact] void should_have_third_level_value_be_lower_cased_property_name() => result.Segments.ToArray()[2].Value.ShouldEqual(third_segment.ToCamelCase());
    [Fact] void should_have_forth_level_be_property_name() => result.Segments.ToArray()[3].ShouldBeOfExactType<PropertyName>();
    [Fact] void should_have_forth_level_value_be_lower_cased_property_name() => result.Segments.ToArray()[3].Value.ShouldEqual(forth_segment.ToCamelCase());
    [Fact] void should_have_fifth_level_be_property_name() => result.Segments.ToArray()[4].ShouldBeOfExactType<ArrayProperty>();
    [Fact] void should_have_fifth_level_value_be_lower_cased_property_name() => result.Segments.ToArray()[4].Value.ShouldEqual(fifth_segment.ToCamelCase());
    [Fact] void should_have_sixth_level_be_property_name() => result.Segments.ToArray()[5].ShouldBeOfExactType<PropertyName>();
    [Fact] void should_have_sixth_level_value_be_lower_cased_property_name() => result.Segments.ToArray()[5].Value.ShouldEqual(sixth_segment.ToCamelCase());
}
