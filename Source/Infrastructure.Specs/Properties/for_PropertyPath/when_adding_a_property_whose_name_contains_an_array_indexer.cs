// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Properties.for_PropertyPath;

public class when_adding_a_property_whose_name_contains_an_array_indexer : Specification
{
    const string Left = "left";
    const string Identifier = "identifier";
    const string Right = $"collection[{Identifier}]";

    PropertyPath _result;

    void Because() => _result = new PropertyPath(Left).AddProperty(Right);

    [Fact] void should_reduce_the_name_to_its_bracketed_part() => _result.Path.ShouldEqual($"{Left}.[{Identifier}]");
    [Fact] void should_hold_one_segment_per_part() => _result.Segments.Count().ShouldEqual(2);
    [Fact] void should_have_last_segment_be_array_index() => _result.LastSegment.ShouldBeOfExactType<ArrayProperty>();
    [Fact] void should_have_last_segment_hold_the_bracketed_identifier() => _result.LastSegment.Value.ShouldEqual(Identifier);
}
