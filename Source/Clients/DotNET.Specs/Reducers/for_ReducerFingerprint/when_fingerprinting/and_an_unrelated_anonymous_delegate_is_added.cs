// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers.for_ReducerFingerprint.when_fingerprinting;

public class and_an_unrelated_anonymous_delegate_is_added : Specification
{
    const string Members = "public ReadModel Reduce(Event @event, ReadModel current) { var apply = (int value = 1) => value + 2; return new(apply(@event.Value)); }";
    Type _first;
    Type _second;
    string _firstHash;
    string _secondHash;

    void Establish()
    {
        _first = given.CompiledReducer.Compile(Members);
        _second = given.CompiledReducer.Compile(Members, before: "public class Unrelated { public object Create() { var callback = (int value = 42) => value; return callback; } }");
    }

    void Because()
    {
        _firstHash = ReducerFingerprint.Create(_first);
        _secondHash = ReducerFingerprint.Create(_second);
    }

    [Fact] void should_actually_shift_the_anonymous_delegate_ordinal() => AnonymousDelegate(_first).Name.ShouldNotEqual(AnonymousDelegate(_second).Name);
    [Fact] void should_produce_the_same_fingerprint() => _firstHash.ShouldEqual(_secondHash);

    static Type AnonymousDelegate(Type reducer) => reducer.Assembly.GetTypes().Single(_ => _.Name.StartsWith("<>f__AnonymousDelegate", StringComparison.Ordinal) && Equals(_.GetMethod("Invoke").GetParameters()[0].DefaultValue, 1));
}
