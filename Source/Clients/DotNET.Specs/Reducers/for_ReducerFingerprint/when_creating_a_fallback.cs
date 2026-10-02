// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers.for_ReducerFingerprint;

public class when_creating_a_fallback : Specification
{
    string _first;
    string _reconnected;
    string _changed;
    string _rebuilt;

    void Because()
    {
        var first = given.CompiledReducer.Compile(given.CompiledReducer.Simple, deterministic: true);
        var rebuilt = given.CompiledReducer.Compile(given.CompiledReducer.Simple, deterministic: true);
        var changed = given.CompiledReducer.Compile(given.CompiledReducer.Simple, before: "public class Unrelated;", deterministic: true);
        _first = ReducerFingerprint.CreateFallback(first);
        _reconnected = ReducerFingerprint.CreateFallback(first);
        _rebuilt = ReducerFingerprint.CreateFallback(rebuilt);
        _changed = ReducerFingerprint.CreateFallback(changed);
    }

    [Fact] void should_preserve_the_sha256_storage_format() => Convert.FromHexString(_first).Length.ShouldEqual(32);
    [Fact] void should_not_replay_on_reconnect_with_the_same_build() => _first.ShouldEqual(_reconnected);
    [Fact] void should_change_when_the_assembly_changes() => _first.ShouldNotEqual(_changed);
    [Fact] void should_keep_the_same_fingerprint_for_identical_deterministic_builds() => _first.ShouldEqual(_rebuilt);
}
