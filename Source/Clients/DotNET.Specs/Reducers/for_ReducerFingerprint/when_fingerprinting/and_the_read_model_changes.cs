// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers.for_ReducerFingerprint.when_fingerprinting;

public class and_the_read_model_changes : Specification
{
    string _first;
    string _second;

    void Because()
    {
        _first = ReducerFingerprint.Create(given.CompiledReducer.Compile(given.CompiledReducer.Simple));
        _second = ReducerFingerprint.Create(given.CompiledReducer.Compile(given.CompiledReducer.Simple.Replace("ReadModel", "OtherReadModel"), model: "OtherReadModel"));
    }

    [Fact] void should_produce_different_fingerprints() => _first.ShouldNotEqual(_second);
}
