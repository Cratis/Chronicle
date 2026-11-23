// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_RecursiveChildProjections;

public class when_schemas_have_matching_structure : given.two_projection_types
{
    void Because() => _result = RecursiveChildProjections.Of(_parent).ToArray();

    [Fact] void should_recognize_the_recursive_child_despite_distinct_schema_instances() => _result.ShouldEqual([_child]);
}
