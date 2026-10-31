// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Sequencing.for_SequencingKeyHash;

public class when_mutating_an_exported_copy : Specification
{
    SequencingKeyHash _hash;
    string _original;

    void Establish()
    {
        _hash = SequencingKeyHash.From(CanonicalSequencingKey.From(given.golden_keys.Key(0)));
        _original = _hash.ToString();
    }

    void Because() => _hash.ToArray()[0] ^= 0xff;

    [Fact] void should_preserve_the_hash() => _hash.ToString().ShouldEqual(_original);
}
