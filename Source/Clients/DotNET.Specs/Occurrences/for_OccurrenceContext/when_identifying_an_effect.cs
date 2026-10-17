// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Occurrences.for_OccurrenceContext;

public class when_identifying_an_effect : Specification
{
    OccurrenceContext _attempt;
    EffectKey _key;

    void Establish() => _attempt = new("occurrence", "trigger", OccurrenceKind.Application, null, DateTimeOffset.UtcNow, null, 3, new("occurrence", 3));

    void Because() => _key = _attempt.EffectKey("send");

    [Fact] void should_use_the_context_identity() => _key.Occurrence.ShouldEqual(_attempt.Id);
    [Fact] void should_keep_the_current_lease() => _key.WrittenBy.ShouldEqual(_attempt.Lease);
    [Fact] void should_deduplicate_against_an_earlier_attempt() => _key.ShouldEqual(new EffectKey("occurrence", "send", new("occurrence", 1)));
}
