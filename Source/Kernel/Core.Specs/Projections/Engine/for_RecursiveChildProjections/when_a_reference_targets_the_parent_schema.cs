// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_RecursiveChildProjections;

public class when_a_reference_targets_the_parent_schema : given.two_schema_references
{
    void Establish() => _parent.TargetReadModelSchema.Returns(Reference("#/$defs/Feature").ActualSchema);

    void Because() => _result = RecursiveChildProjections.Of(_parent).ToArray();

    [Fact] void should_match_the_reference_with_its_resolved_target() => _result.ShouldEqual([_child]);
}
