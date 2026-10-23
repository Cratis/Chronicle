// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Observation.for_PartitionReplayabilityClassifier;

public class when_from_every_uses_a_null_key : given.a_definition
{
    protected override ProjectionDefinition Change(ProjectionDefinition definition)
    {
        return definition with { FromEvery = definition.FromEvery with { Key = null! } };
    }

    void Because() => Result = PartitionReplayabilityClassifier.Classify(Type, Definition);

    [Fact] void should_be_closed() => Result.ShouldEqual(PartitionReplayability.Closed);
}
