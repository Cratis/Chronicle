// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_RecursiveChildProjections;

public class when_schema_ids_differ : given.two_projection_types
{
    void Establish()
    {
        _parentSchema.Node["$id"] = "https://schemas.example/feature";
        _childSchema.Node["$id"] = "https://schemas.example/unrelated-feature";
    }

    void Because() => _result = RecursiveChildProjections.Of(_parent).ToArray();

    [Fact] void should_not_match_structurally_identical_but_differently_identified_types() => _result.ShouldBeEmpty();
}
