// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Properties.for_PropertyPath;

public class when_adding_this_accessor_to_an_empty_property : Specification
{
    PropertyPath _result;

    void Because() => _result = PropertyPath.Root.AddThisAccessor();

    [Fact] void should_keep_the_separator_for_the_empty_segment() => _result.Path.ShouldEqual($".{PropertyPath.ThisAccessorValue}");
    [Fact] void should_hold_the_empty_segment_and_the_this_accessor() => _result.Segments.Count().ShouldEqual(2);
    [Fact] void should_have_last_segment_be_this_accessor() => _result.LastSegment.ShouldBeOfExactType<ThisAccessor>();
}
