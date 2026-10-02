// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers.for_ReducerFingerprint.when_fingerprinting;

public class and_an_unrelated_anonymous_type_is_added : Specification
{
    const string Members = "public ReadModel Reduce(Event @event, ReadModel current) => new(new { Original = @event.Value, Current = current.Value }.GetHashCode());";
    Type _first;
    Type _second;
    string _firstHash;
    string _secondHash;

    void Establish()
    {
        _first = given.CompiledReducer.Compile(Members);
        _second = given.CompiledReducer.Compile(Members, before: "public class Unrelated { public object Create() => new { Noise = 42 }; }");
    }

    void Because()
    {
        _firstHash = ReducerFingerprint.Create(_first);
        _secondHash = ReducerFingerprint.Create(_second);
    }

    [Fact] void should_actually_shift_the_anonymous_type_ordinal() => AnonymousType(_first).Name.ShouldNotEqual(AnonymousType(_second).Name);
    [Fact] void should_produce_the_same_fingerprint() => _firstHash.ShouldEqual(_secondHash);

    static Type AnonymousType(Type reducer) => reducer.Assembly.GetTypes().Single(_ => _.Name.StartsWith("<>f__AnonymousType", StringComparison.Ordinal) && _.GetProperty("Original") is not null);
}
