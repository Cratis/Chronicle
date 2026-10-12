// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Sequencing.for_CanonicalSequencingKey.when_trying_to_canonicalize;

public class with_malformed_text : Specification
{
    bool _accepted;
    CanonicalSequencingKey? _key;
    CanonicalSequencingKeyRejection _rejection;

    void Because() => _accepted = CanonicalSequencingKey.TryFrom(new ValueSequencingKey(["\uD800"]), out _key, out _rejection);

    [Fact] void should_refuse_canonicalization() => _accepted.ShouldBeFalse();
    [Fact] void should_not_substitute_another_key() => _key.ShouldBeNull();
    [Fact] void should_report_the_reason() => _rejection.ShouldEqual(CanonicalSequencingKeyRejection.InvalidText);
}
