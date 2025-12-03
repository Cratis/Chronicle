// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Properties.for_PropertyPath;

public class when_creating_complex_path_with_nested_properties_and_array_indexers : Specification
{
    const string FirstSegment = "FirstSegment";
    const string SecondSegment = "SecondSegment";
    const string ThirdSegment = "ThirdSegment";
    const string ForthSegment = "ForthSegment";
    const string FifthSegment = "FifthSegment";
    const string SixthSegment = "SixthSegment";

    PropertyPath _result;

    void Because() => _result = new PropertyPath($"{FirstSegment}.[{SecondSegment}].{ThirdSegment}.{ForthSegment}.[{FifthSegment}].{SixthSegment}");

    [Fact] void should_have_six_segments() => _result.Segments.Count().ShouldEqual(6);
    [Fact] void should_have_first_level_be_property_name() => _result.Segments.First().ShouldBeOfExactType<PropertyName>();
    [Fact] void should_have_first_level_value_be_lower_cased_property_name() => _result.Segments.First().Value.ShouldEqual(FirstSegment);
    [Fact] void should_have_second_level_be_array_index() => _result.Segments.ToArray()[1].ShouldBeOfExactType<ArrayProperty>();
    [Fact] void should_have_second_level_value_be_lower_cased_property_name() => _result.Segments.ToArray()[1].Value.ShouldEqual(SecondSegment);
    [Fact] void should_have_third_level_be_property_name() => _result.Segments.ToArray()[2].ShouldBeOfExactType<PropertyName>();
    [Fact] void should_have_third_level_value_be_lower_cased_property_name() => _result.Segments.ToArray()[2].Value.ShouldEqual(ThirdSegment);
    [Fact] void should_have_forth_level_be_property_name() => _result.Segments.ToArray()[3].ShouldBeOfExactType<PropertyName>();
    [Fact] void should_have_forth_level_value_be_lower_cased_property_name() => _result.Segments.ToArray()[3].Value.ShouldEqual(ForthSegment);
    [Fact] void should_have_fifth_level_be_property_name() => _result.Segments.ToArray()[4].ShouldBeOfExactType<ArrayProperty>();
    [Fact] void should_have_fifth_level_value_be_lower_cased_property_name() => _result.Segments.ToArray()[4].Value.ShouldEqual(FifthSegment);
    [Fact] void should_have_sixth_level_be_property_name() => _result.Segments.ToArray()[5].ShouldBeOfExactType<PropertyName>();
    [Fact] void should_have_sixth_level_value_be_lower_cased_property_name() => _result.Segments.ToArray()[5].Value.ShouldEqual(SixthSegment);
}
