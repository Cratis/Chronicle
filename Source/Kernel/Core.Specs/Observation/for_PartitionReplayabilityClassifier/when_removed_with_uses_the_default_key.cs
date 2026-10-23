// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Observation.for_PartitionReplayabilityClassifier;

public class when_removed_with_uses_the_default_key : given.a_definition
{
    protected override ProjectionDefinition Change(ProjectionDefinition definition)
    {
        return definition with { RemovedWith = new Dictionary<EventType, RemovedWithDefinition> { [Event] = new(new(""), null) } };
    }

    void Because() => Result = PartitionReplayabilityClassifier.Classify(Type, Definition);

    [Fact] void should_be_closed() => Result.ShouldEqual(PartitionReplayability.Closed);
}
