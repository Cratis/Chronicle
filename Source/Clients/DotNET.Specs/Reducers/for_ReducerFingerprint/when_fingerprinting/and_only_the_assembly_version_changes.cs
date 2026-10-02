// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers.for_ReducerFingerprint.when_fingerprinting;

public class and_only_the_assembly_version_changes : Specification
{
    Type _first;
    Type _second;
    string _firstHash;
    string _secondHash;

    void Establish()
    {
        const string Members = "public ReadModel Reduce(Event @event, ReadModel current) => new(Helper.Apply(@event.Value) + Helper<int>.Apply(current.Value));";
        const string Helpers = "public static class Helper { public static int Apply(int value) => value; } public static class Helper<T> { public static int Apply(int value) => value; }";
        _first = given.CompiledReducer.Compile(Members, before: Helpers);
        _second = given.CompiledReducer.Compile(Members, version: "2.3.4.5", before: Helpers);
    }

    void Because()
    {
        _firstHash = ReducerFingerprint.Create(_first);
        _secondHash = ReducerFingerprint.Create(_second);
    }

    [Fact] void should_have_different_assembly_versions() => _first.Assembly.GetName().Version.ShouldNotEqual(_second.Assembly.GetName().Version);
    [Fact] void should_have_different_build_identifiers() => _first.Module.ModuleVersionId.ShouldNotEqual(_second.Module.ModuleVersionId);
    [Fact] void should_produce_the_same_fingerprint() => _firstHash.ShouldEqual(_secondHash);
    [Fact] void should_keep_the_existing_storage_format() => _firstHash.Length.ShouldEqual(64);
}
