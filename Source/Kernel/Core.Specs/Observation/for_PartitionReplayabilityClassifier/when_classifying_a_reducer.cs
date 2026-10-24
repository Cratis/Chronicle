// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_PartitionReplayabilityClassifier;

public class when_classifying_a_reducer : given.a_definition
{
    protected override ObserverType Type => ObserverType.Reducer;

    void Because() => Result = PartitionReplayabilityClassifier.Classify(Type, Definition);

    [Fact] void should_be_closed() => Result.ShouldEqual(PartitionReplayability.Closed);
}
