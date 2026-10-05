// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers.for_ReducerFingerprint.when_fingerprinting;

public class and_the_anonymous_delegate_shape_changes : Specification
{
    const string Members = "public ReadModel Reduce(Event @event, ReadModel current) { var apply = (int value = 1) => value + 2; return new(apply(@event.Value)); }";
    string _original;
    string _differentDefault;
    string _differentSignature;

    void Because()
    {
        _original = ReducerFingerprint.Create(given.CompiledReducer.Compile(Members));
        _differentDefault = ReducerFingerprint.Create(given.CompiledReducer.Compile(Members.Replace("value = 1", "value = 42")));
        _differentSignature = ReducerFingerprint.Create(given.CompiledReducer.Compile(Members.Replace("int value", "long value").Replace("value + 2", "(int)value + 2")));
    }

    [Fact] void should_distinguish_delegates_with_different_defaults_even_when_the_call_supplies_a_value() => _differentDefault.ShouldNotEqual(_original);
    [Fact] void should_distinguish_delegates_with_different_signatures() => _differentSignature.ShouldNotEqual(_original);
}
