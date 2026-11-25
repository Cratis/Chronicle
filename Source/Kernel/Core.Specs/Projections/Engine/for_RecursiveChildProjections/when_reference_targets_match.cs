// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_RecursiveChildProjections;

public class when_reference_targets_match : given.two_schema_references
{
    void Because() => _result = RecursiveChildProjections.Of(_parent).ToArray();

    [Fact] void should_recognize_the_recursive_child() => _result.ShouldEqual([_child]);
}
