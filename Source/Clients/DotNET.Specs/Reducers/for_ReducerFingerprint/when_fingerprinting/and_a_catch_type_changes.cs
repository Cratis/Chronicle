// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers.for_ReducerFingerprint.when_fingerprinting;

public class and_a_catch_type_changes : Specification
{
    const string Members = """
        public ReadModel Reduce(Event @event, ReadModel current)
        {
            try { return new(checked(current.Value + @event.Value)); }
            catch (OverflowException) { return current; }
        }
        """;
    string _first;
    string _second;

    void Because()
    {
        _first = ReducerFingerprint.Create(given.CompiledReducer.Compile(Members));
        _second = ReducerFingerprint.Create(given.CompiledReducer.Compile(Members.Replace("OverflowException", "Exception")));
    }

    [Fact] void should_produce_different_fingerprints() => _first.ShouldNotEqual(_second);
}
