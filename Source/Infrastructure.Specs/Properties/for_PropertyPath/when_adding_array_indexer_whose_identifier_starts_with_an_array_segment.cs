// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Properties.for_PropertyPath;

public class when_adding_array_indexer_whose_identifier_starts_with_an_array_segment : Specification
{
    const string FirstSegment = "FirstSegment";
    const string SecondSegment = "SecondSegment";

    PropertyPath _result;

    void Because() => _result = PropertyPath.Root.AddArrayIndex($"[{FirstSegment}].{SecondSegment}");

    [Fact] void should_render_the_leading_segment_without_its_brackets() => _result.Path.ShouldEqual($"{FirstSegment}.[{SecondSegment}]");
    [Fact] void should_have_first_segment_be_a_property_name() => _result.Segments.First().ShouldBeOfExactType<PropertyName>();
    [Fact] void should_have_last_segment_be_array_index() => _result.LastSegment.ShouldBeOfExactType<ArrayProperty>();
}
