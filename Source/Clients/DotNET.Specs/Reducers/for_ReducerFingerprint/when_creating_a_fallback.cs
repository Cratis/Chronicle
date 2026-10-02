// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers.for_ReducerFingerprint;

public class when_creating_a_fallback : Specification
{
    string _first;
    string _reconnected;
    string _rebuilt;

    void Because()
    {
        var first = given.CompiledReducer.Compile(given.CompiledReducer.Simple);
        var rebuilt = given.CompiledReducer.Compile(given.CompiledReducer.Simple);
        _first = ReducerFingerprint.CreateFallback(first);
        _reconnected = ReducerFingerprint.CreateFallback(first);
        _rebuilt = ReducerFingerprint.CreateFallback(rebuilt);
    }

    [Fact] void should_preserve_the_sha256_storage_format() => Convert.FromHexString(_first).Length.ShouldEqual(32);
    [Fact] void should_not_replay_on_reconnect_with_the_same_build() => _first.ShouldEqual(_reconnected);
    [Fact] void should_replay_on_the_next_build_even_without_a_version_change() => _first.ShouldNotEqual(_rebuilt);
}
