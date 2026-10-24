// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_PartitionReplayabilityClassifier;

public class when_from_uses_the_default_key : given.a_definition
{
    void Because() => Result = PartitionReplayabilityClassifier.Classify(Type, Definition);

    [Fact] void should_be_closed() => Result.ShouldEqual(PartitionReplayability.Closed);
}
