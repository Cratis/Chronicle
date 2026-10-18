// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.Idempotency.for_AppendFingerprint;

public class when_the_routing_differs : given.an_append_intent
{
    string[] _hashes;
    void Because()
    {
        AppendFingerprintInput[] variants = [
            _input with { EventType = _input.EventType with { Id = "other" } },
            _input with { EventSourceId = "other" },
            _input with { EventSourceType = "other" },
            _input with { EventStreamType = "other" },
            _input with { EventStreamId = "other" },
            _input with { EventSource = "other" },
            _input with { Subject = "other" }];
        _hashes = variants.Select(variant => Compute(variant).Plain).ToArray();
    }
    [Fact] void should_include_every_routing_field_in_the_plain_part() => _hashes.ShouldEachConformTo(hash => hash != _original.Plain);
    [Fact] void should_keep_the_fields_distinct() => _hashes.Distinct(StringComparer.Ordinal).Count().ShouldEqual(_hashes.Length);
}
