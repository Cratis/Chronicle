// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers.for_ReducerFingerprint.when_fingerprinting;

public class and_generic_signatures_are_recompiled : Specification
{
    const string Members = """
        public Task<Dictionary<string, List<ReadModel[]>>> Reduce(List<Event> events, ReadModel current) =>
            Task.FromResult(new Dictionary<string, List<ReadModel[]>> { [typeof(Event).Name] = new() { new[] { current } } });
        private T Identity<T>(ref T value) => value;
        """;
    string _first;
    string _second;

    void Because()
    {
        _first = ReducerFingerprint.Create(given.CompiledReducer.Compile(Members));
        _second = ReducerFingerprint.Create(given.CompiledReducer.Compile(Members, version: "2.0.0.0", culture: "fr-FR"));
    }

    [Fact] void should_produce_the_same_fingerprint() => _first.ShouldEqual(_second);
}
