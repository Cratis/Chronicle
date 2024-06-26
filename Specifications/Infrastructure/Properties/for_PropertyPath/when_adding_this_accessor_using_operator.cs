// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Properties.for_PropertyPath;

public class when_adding_this_accessor_using_operator : Specification
{
    const string first_segment = "FirstSegment";
    const string second_segment = "SecondSegment";

    PropertyPath initial;
    PropertyPath result;

    void Establish() => initial = new PropertyPath($"{first_segment}.{second_segment}");

    void Because() => result = initial + "$this";

    [Fact] void should_have_last_segment_be_array_index() => result.LastSegment.ShouldBeOfExactType<ThisAccessor>();
}
