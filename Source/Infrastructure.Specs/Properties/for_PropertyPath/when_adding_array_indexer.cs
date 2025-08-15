// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Properties.for_PropertyPath;

public class when_adding_array_indexer : Specification
{
    const string FirstSegment = "FirstSegment";
    const string SecondSegment = "SecondSegment";
    const string ThirdSegment = "ThirdSegment";

    PropertyPath _initial;
    PropertyPath _result;

    void Establish() => _initial = new PropertyPath($"{FirstSegment}.{SecondSegment}");

    void Because() => _result = _initial.AddArrayIndex(ThirdSegment);

    [Fact] void should_have_last_segment_be_array_index() => _result.LastSegment.ShouldBeOfExactType<ArrayProperty>();
    [Fact] void should_have_last_segment_have_camel_case_identifier() => _result.LastSegment.Value.ShouldEqual(ThirdSegment);
}
