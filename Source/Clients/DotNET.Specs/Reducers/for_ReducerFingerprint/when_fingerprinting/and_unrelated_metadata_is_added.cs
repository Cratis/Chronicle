// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers.for_ReducerFingerprint.when_fingerprinting;

public class and_unrelated_metadata_is_added : Specification
{
    const string Members = """
        public ReadModel Reduce(Event @event, ReadModel current) =>
            new(current.Value + @event.Value + "constant".Length + typeof(Event).Name.Length);
        """;
    Type _first;
    Type _second;
    string _firstHash;
    string _secondHash;

    void Establish()
    {
        _first = given.CompiledReducer.Compile(Members);
        _second = given.CompiledReducer.Compile(Members, before: "public class Unrelated { public string Value = \"different string\"; public int GetValue() => Value.Length; }");
    }

    void Because()
    {
        _firstHash = ReducerFingerprint.Create(_first);
        _secondHash = ReducerFingerprint.Create(_second);
    }

    [Fact] void should_actually_have_different_raw_il() => _first.GetMethod("Reduce").GetMethodBody().GetILAsByteArray().SequenceEqual(_second.GetMethod("Reduce").GetMethodBody().GetILAsByteArray()).ShouldBeFalse();
    [Fact] void should_produce_the_same_fingerprint() => _firstHash.ShouldEqual(_secondHash);
}
