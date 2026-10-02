// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers.for_ReducerFingerprint.when_fingerprinting;

public class and_the_anonymous_type_shape_changes : Specification
{
    string _original;
    string _renamed;
    string _reordered;
    string _differentType;

    void Because()
    {
        _original = Fingerprint("new { First = @event.Value, Second = current.Value }");
        _renamed = Fingerprint("new { Renamed = @event.Value, Second = current.Value }");
        _reordered = Fingerprint("new { Second = @event.Value, First = current.Value }");
        _differentType = Fingerprint("new { First = (long)@event.Value, Second = current.Value }");
    }

    [Fact] void should_detect_renamed_properties() => _renamed.ShouldNotEqual(_original);
    [Fact] void should_detect_reordered_properties() => _reordered.ShouldNotEqual(_original);
    [Fact] void should_detect_changed_property_types() => _differentType.ShouldNotEqual(_original);

    static string Fingerprint(string expression) => ReducerFingerprint.Create(given.CompiledReducer.Compile($"public ReadModel Reduce(Event @event, ReadModel current) => new(({expression}).GetHashCode());"));
}
