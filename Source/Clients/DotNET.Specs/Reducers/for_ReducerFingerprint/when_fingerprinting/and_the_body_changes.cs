// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers.for_ReducerFingerprint.when_fingerprinting;

public class and_the_body_changes : Specification
{
    string _original;
    string[] _changed;

    void Because()
    {
        _original = Fingerprint("Math.Abs(@event.Value) + 42 + \"first\".Length");
        _changed =
        [
            Fingerprint("Math.Sign(@event.Value) + 42 + \"first\".Length"),
            Fingerprint("Math.Abs(@event.Value) + 43 + \"first\".Length"),
            Fingerprint("Math.Abs(@event.Value) + 42 + \"other\".Length"),
            Fingerprint("@event.Value > 0 ? Math.Abs(@event.Value) + 42 + \"first\".Length : 0")
        ];
    }

    [Fact] void should_detect_a_different_call() => _changed[0].ShouldNotEqual(_original);
    [Fact] void should_detect_a_different_constant() => _changed[1].ShouldNotEqual(_original);
    [Fact] void should_detect_a_different_string_literal() => _changed[2].ShouldNotEqual(_original);
    [Fact] void should_detect_a_different_branch() => _changed[3].ShouldNotEqual(_original);

    static string Fingerprint(string expression) => ReducerFingerprint.Create(given.CompiledReducer.Compile($"public ReadModel Reduce(Event @event, ReadModel current) => new({expression});"));
}
