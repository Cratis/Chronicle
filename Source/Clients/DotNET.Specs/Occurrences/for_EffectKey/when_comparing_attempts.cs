// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Occurrences.for_EffectKey;

public class when_comparing_attempts : Specification
{
    EffectKey _first;
    EffectKey _retry;

    void Establish() => _first = new("occurrence", "send", new("occurrence", 1));
    void Because() => _retry = new("occurrence", "send", new("occurrence", 2));

    [Fact] void should_retain_identity_across_attempts() => _retry.ShouldEqual(_first);
    [Fact] void should_retain_the_hash_code() => _retry.GetHashCode().ShouldEqual(_first.GetHashCode());
    [Fact] void should_retain_the_external_idempotency_key() => _retry.Value.ShouldEqual(_first.Value);
    [Fact] void should_keep_the_writing_lease() => _retry.WrittenBy.Epoch.ShouldEqual(2L);
}
