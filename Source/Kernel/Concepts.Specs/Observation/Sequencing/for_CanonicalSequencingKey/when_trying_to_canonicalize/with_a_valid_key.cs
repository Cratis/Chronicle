// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Sequencing.for_CanonicalSequencingKey.when_trying_to_canonicalize;

public class with_a_valid_key : Specification
{
    bool _accepted;
    CanonicalSequencingKey? _key;
    CanonicalSequencingKeyRejection _rejection;

    void Because() => _accepted = CanonicalSequencingKey.TryFrom(given.golden_keys.Key(0), out _key, out _rejection);

    [Fact] void should_accept_canonicalization() => _accepted.ShouldBeTrue();
    [Fact] void should_provide_the_canonical_key() => _key.ShouldEqual(CanonicalSequencingKey.From(given.golden_keys.Key(0)));
    [Fact] void should_report_no_rejection() => _rejection.ShouldEqual(CanonicalSequencingKeyRejection.None);
}
