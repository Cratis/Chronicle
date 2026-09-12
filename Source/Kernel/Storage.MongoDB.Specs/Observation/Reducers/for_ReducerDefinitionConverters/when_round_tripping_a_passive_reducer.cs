// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.Reducers.for_ReducerDefinitionConverters;

public class when_round_tripping_a_passive_reducer : Specification
{
    Concepts.Observation.Reducers.ReducerDefinition _result;

    void Because()
    {
        var definition = new Concepts.Observation.Reducers.ReducerDefinition("reducer", EventSequenceId.Log, [], "read-model", false, [], Hash: "implementation");
        _result = definition.ToMongoDB().ToKernel();
    }

    [Fact] void should_preserve_the_implementation_fingerprint() => _result.Hash.ShouldEqual("implementation");
    [Fact] void should_remain_passive() => _result.IsActive.ShouldBeFalse();
}
