// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.EventSequences.Idempotency.for_AppendFingerprint;

public class when_comparing_a_plain_conflict : given.an_append_intent
{
    IEncryptionKeyStorage _keys;
    FingerprintComparisonResult _comparison;
    void Establish()
    {
        Protect();
        _input.Content["count"] = 2;
        _keys = Substitute.For<IEncryptionKeyStorage>();
    }
    async Task Because() => (await AppendFingerprintComparison.Compare(_original, _input, _keys, Substitute.For<IEvictEncryptionKeyCache>(), "store", "ns")).TryGetResult(out _comparison).ShouldBeTrue();
    [Fact] void should_report_content() => _comparison.Reason.ShouldEqual(FingerprintConflictReason.Content);
    [Fact] void should_conflict() => _comparison.Status.ShouldEqual(FingerprintComparisonStatus.Conflict);
    [Fact] void should_not_read_any_key() => _keys.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_retain_the_original_event_type() => _comparison.StoredEventType.ShouldEqual(_original.EventType);
}
