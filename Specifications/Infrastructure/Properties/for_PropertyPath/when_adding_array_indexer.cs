// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Strings;

namespace Aksio.Cratis.Properties.for_PropertyPath;

public class when_adding_array_indexer : Specification
{
    const string first_segment = "FirstSegment";
    const string second_segment = "SecondSegment";
    const string third_segment = "ThirdSegment";

    PropertyPath initial;
    PropertyPath result;

    void Establish() => initial = new PropertyPath($"{first_segment}.{second_segment}");

    void Because() => result = initial.AddArrayIndex(third_segment);

    [Fact] void should_have_last_segment_be_array_index() => result.LastSegment.ShouldBeOfExactType<ArrayProperty>();
    [Fact] void should_have_last_segment_have_camel_case_identifier() => result.LastSegment.Value.ShouldEqual(third_segment.ToCamelCase());
}


public class when_adding_this_accessor : Specification
{
    const string first_segment = "FirstSegment";
    const string second_segment = "SecondSegment";

    PropertyPath initial;
    PropertyPath result;

    void Establish() => initial = new PropertyPath($"{first_segment}.{second_segment}");

    void Because() => result = initial.AddThisAccessor();

    [Fact] void should_have_last_segment_be_this_accessor() => result.LastSegment.ShouldBeOfExactType<ThisAccessor>();
}
