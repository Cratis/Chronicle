// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Observation.for_PartitionReplayabilityClassifier;

public class when_there_is_a_nested_object : given.a_definition
{
    protected override ProjectionDefinition Change(ProjectionDefinition definition)
    {
        return definition with { Nested = new Dictionary<PropertyPath, ChildrenDefinition> { ["details"] = new("id", definition.From, definition.Join, definition.Children, definition.FromEvery, definition.RemovedWith, definition.RemovedWithJoin) } };
    }

    void Because() => Result = PartitionReplayabilityClassifier.Classify(Type, Definition);

    [Fact] void should_be_open() => Result.ShouldEqual(PartitionReplayability.Open);
}
