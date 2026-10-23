// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_PartitionReplayabilityClassifier;

public class when_the_projection_definition_is_missing : Specification
{
    PartitionReplayability _result;

    void Because() => _result = PartitionReplayabilityClassifier.Classify(ObserverType.Projection);

    [Fact] void should_be_open() => _result.ShouldEqual(PartitionReplayability.Open);
}
