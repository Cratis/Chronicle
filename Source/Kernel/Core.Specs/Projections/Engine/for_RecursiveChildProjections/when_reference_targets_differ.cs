// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_RecursiveChildProjections;

public class when_reference_targets_differ : given.two_schema_references
{
    void Establish() => _child.TargetReadModelSchema.Returns(Reference("#/$defs/OtherFeature"));

    void Because() => _result = RecursiveChildProjections.Of(_parent).ToArray();

    [Fact] void should_not_match_distinct_targets_even_when_their_structure_matches() => _result.ShouldBeEmpty();
}
