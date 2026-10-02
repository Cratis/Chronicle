// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers.for_ReducerFingerprint.when_fingerprinting;

public class and_async_and_lambda_bodies_are_recompiled : Specification
{
    const string Members = """
        public async Task<ReadModel> Reduce(Event @event, ReadModel current)
        {
            await Task.Yield();
            var values = new[] { @event.Value, current.Value };
            return new(values.Select(value => value + current.Value).Select(value => value * 2).Sum());
        }
        """;
    string _first;
    string _second;

    void Because()
    {
        _first = ReducerFingerprint.Create(given.CompiledReducer.Compile(Members));
        _second = ReducerFingerprint.Create(given.CompiledReducer.Compile(Members, version: "2.0.0.0", before: "public class Unrelated { public async Task<int> Run() { await Task.Yield(); return new[] { 1 }.Select(x => x * 3).Sum(); } }"));
    }

    [Fact] void should_produce_the_same_fingerprint() => _first.ShouldEqual(_second);
}
