// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Properties.for_PropertyPath;

public class when_adding_this_accessor : Specification
{
    const string FirstSegment = "FirstSegment";
    const string SecondSegment = "SecondSegment";

    PropertyPath _initial;
    PropertyPath _result;

    void Establish() => _initial = new PropertyPath($"{FirstSegment}.{SecondSegment}");

    void Because() => _result = _initial.AddThisAccessor();

    [Fact] void should_have_last_segment_be_this_accessor() => _result.LastSegment.ShouldBeOfExactType<ThisAccessor>();
}
