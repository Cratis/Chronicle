// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Sequencing.for_CanonicalSequencingKey;

public class when_mutating_an_exported_copy : Specification
{
    CanonicalSequencingKey _key;
    string _original;

    void Establish()
    {
        _key = CanonicalSequencingKey.From(given.golden_keys.Key(0));
        _original = Convert.ToHexStringLower(_key.Span);
    }

    void Because() => _key.ToArray()[0] ^= 0xff;

    [Fact] void should_preserve_the_canonical_bytes() => Convert.ToHexStringLower(_key.Span).ShouldEqual(_original);
}
