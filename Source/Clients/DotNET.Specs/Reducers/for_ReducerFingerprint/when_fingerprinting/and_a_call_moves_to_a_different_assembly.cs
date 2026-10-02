// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers.for_ReducerFingerprint.when_fingerprinting;

public class and_a_call_moves_to_a_different_assembly : Specification
{
    string _left;
    string _right;
    string _leftGeneric;
    string _rightGeneric;

    void Because()
    {
        (string Name, byte[] Image)[] dependencies = [("Left", given.CollisionAssembly.Compile("Left")), ("Right", given.CollisionAssembly.Compile("Right"))];
        _left = Fingerprint("Left", "Helper");
        _right = Fingerprint("Right", "Helper");
        _leftGeneric = Fingerprint("Left", "Helper<int>");
        _rightGeneric = Fingerprint("Right", "Helper<int>");

        string Fingerprint(string assembly, string helper) => ReducerFingerprint.Create(given.CompiledReducer.Compile(
            $"public ReadModel Reduce(Event @event, ReadModel current) => new({assembly}::Collision.{helper}.Apply(@event.Value));",
            dependencies: dependencies));
    }

    [Fact] void should_distinguish_same_named_members_in_different_assemblies() => _left.ShouldNotEqual(_right);
    [Fact] void should_distinguish_same_named_generic_types_in_different_assemblies() => _leftGeneric.ShouldNotEqual(_rightGeneric);
}
