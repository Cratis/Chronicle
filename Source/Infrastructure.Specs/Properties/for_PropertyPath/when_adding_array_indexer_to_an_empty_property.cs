// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Properties.for_PropertyPath;

public class when_adding_array_indexer_to_an_empty_property : Specification
{
    const string Identifier = "Identifier";

    PropertyPath _result;

    void Because() => _result = PropertyPath.Root.AddArrayIndex(Identifier);

    [Fact] void should_hold_only_the_array_indexer() => _result.Path.ShouldEqual($"[{Identifier}]");
    [Fact] void should_hold_a_single_segment() => _result.Segments.Count().ShouldEqual(1);
}
