// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers.for_ReducerFingerprint.when_fingerprinting;

public class and_implementations_are_different : Specification
{
    string _first;
    string _second;

    void Because()
    {
        _first = ReducerFingerprint.Create(typeof(FirstReducer));
        _second = ReducerFingerprint.Create(typeof(SecondReducer));
    }

    [Fact] void should_produce_different_fingerprints() => _first.ShouldNotEqual(_second);

    class FirstReducer
    {
        public int Reduce(string @event, int current) => current + @event.Length;
    }

    class SecondReducer
    {
        public int Reduce(string @event, int current) => current - @event.Length;
    }
}
