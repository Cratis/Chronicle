// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Sequencing.for_SequencingKeyIdentity;

public class when_mutating_input_buffers_after_restoring : Specification
{
    SequencingKeyIdentity _original;
    SequencingKeyIdentity _restored;
    byte[] _key;
    byte[] _hash;

    void Establish()
    {
        _original = SequencingKeyIdentity.For(given.golden_keys.Key(0));
        _key = _original.Key.ToArray();
        _hash = _original.Hash.ToArray();
        _restored = SequencingKeyIdentity.FromStored(_original.Kind, _hash, _key, _original.Slice.Value);
    }

    void Because()
    {
        _key[0] ^= 0xff;
        _hash[0] ^= 0xff;
    }

    [Fact] void should_preserve_the_restored_identity() => _restored.ShouldEqual(_original);
}
