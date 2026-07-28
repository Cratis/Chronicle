// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Properties.for_PropertyPath;

public class when_composing_two_property_paths : Specification
{
    const string Left = "first.[second]";
    const string Right = "third.$this";

    PropertyPath _left;
    PropertyPath _right;
    PropertyPath _result;
    IPropertyPathSegment[] _segments;

    void Establish()
    {
        _left = new PropertyPath(Left);
        _right = new PropertyPath(Right);
    }

    void Because()
    {
        _result = _left + _right;
        _segments = [.. _result.Segments];
    }

    [Fact] void should_combine_with_dot_separator() => _result.Path.ShouldEqual($"{Left}.{Right}");
    [Fact] void should_hold_every_segment_from_both_sides() => _segments.Length.ShouldEqual(4);
    [Fact] void should_carry_the_left_hand_segments_over_without_reparsing_them() => _left.Segments.SequenceEqual(_segments.Take(2), ReferenceEqualityComparer.Instance).ShouldBeTrue();
    [Fact] void should_carry_the_right_hand_segments_over_without_reparsing_them() => _right.Segments.SequenceEqual(_segments.Skip(2), ReferenceEqualityComparer.Instance).ShouldBeTrue();
}
