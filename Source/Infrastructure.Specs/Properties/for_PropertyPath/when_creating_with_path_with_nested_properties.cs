// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Properties.for_PropertyPath;

public class when_creating_with_path_with_nested_properties : Specification
{
    const string FirstSegment = "FirstSegment";
    const string SecondSegment = "SecondSegment";

    PropertyPath _result;

    void Because() => _result = new PropertyPath($"{FirstSegment}.{SecondSegment}");

    [Fact] void should_have_two_segments() => _result.Segments.Count().ShouldEqual(2);
    [Fact] void should_have_first_level_be_property_name() => _result.Segments.First().ShouldBeOfExactType<PropertyName>();
    [Fact] void should_have_first_level_value_be_lower_cased_property_name() => _result.Segments.First().Value.ShouldEqual(FirstSegment);
    [Fact] void should_have_second_level_be_property_name() => _result.Segments.ToArray()[1].ShouldBeOfExactType<PropertyName>();
    [Fact] void should_have_second_level_value_be_lower_cased_property_name() => _result.Segments.ToArray()[1].Value.ShouldEqual(SecondSegment);
}
