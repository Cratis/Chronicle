// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_RecursiveChildProjections;

public class when_a_schema_has_no_title : given.two_projection_types
{
    void Establish()
    {
        _parentSchema.Title = null;
        _childSchema.Title = null;
    }

    void Because() => _result = RecursiveChildProjections.Of(_parent).ToArray();

    [Fact] void should_not_infer_recursion_from_unnamed_structures() => _result.ShouldBeEmpty();
}
