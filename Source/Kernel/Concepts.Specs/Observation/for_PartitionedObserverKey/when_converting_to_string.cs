// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Grains.Observation.for_PartitionedObserverKey;

public class when_converting_to_string : Specification
{
    PartitionedObserverKey _input;
    string _result;

    void Establish() => _input = new(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

    void Because() => _result = _input.ToString();

    [Fact] void should_combine_correctly() => _result.ShouldEqual($"{_input.EventStore}{KeyHelper.Separator}{_input.Namespace}{KeyHelper.Separator}{_input.EventSequenceId}{KeyHelper.Separator}{_input.EventSourceId}");
}
