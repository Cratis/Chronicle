// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers.for_ReducerFingerprint.when_fingerprinting;

public class and_a_function_pointer_signature_is_recompiled : Specification
{
    const string Members = """
        public unsafe ReadModel Reduce(Event @event, ReadModel current)
        {
            delegate*<Event, ReadModel, ReadModel> reduce = &Apply;
            return reduce(@event, current);
        }
        private static ReadModel Apply(Event @event, ReadModel current) => new(current.Value + @event.Value);
        """;
    string _first;
    string _second;

    void Because()
    {
        _first = ReducerFingerprint.Create(given.CompiledReducer.Compile(Members));
        _second = ReducerFingerprint.Create(given.CompiledReducer.Compile(Members, version: "2.0.0.0", before: "public record Unrelated(int Value);"));
    }

    [Fact] void should_produce_the_same_fingerprint() => _first.ShouldEqual(_second);
}
