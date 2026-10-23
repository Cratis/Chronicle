// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Observation.for_PartitionReplayabilityClassifier;

public class when_derivatives_use_a_property_key : given.a_definition
{
    protected override ProjectionDefinition Change(ProjectionDefinition definition)
    {
        return definition with { FromDerivatives = [new([Event], new(new Dictionary<PropertyPath, string>(), "customerId", null))] };
    }

    void Because() => Result = PartitionReplayabilityClassifier.Classify(Type, Definition);

    [Fact] void should_be_open() => Result.ShouldEqual(PartitionReplayability.Open);
}
