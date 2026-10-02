// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers.for_ReducerFingerprint.when_fingerprinting;

public class and_a_function_pointer_calling_convention_changes : Specification
{
    const string Members = "public unsafe ReadModel Reduce(delegate*<int, int> callback, ReadModel current) => current;";
    string _first;
    string _second;

    void Because()
    {
        _first = ReducerFingerprint.Create(given.CompiledReducer.Compile(Members));
        _second = ReducerFingerprint.Create(given.CompiledReducer.Compile(Members.Replace("delegate*<", "delegate* unmanaged<")));
    }

    [Fact] void should_produce_different_fingerprints() => _first.ShouldNotEqual(_second);
}
