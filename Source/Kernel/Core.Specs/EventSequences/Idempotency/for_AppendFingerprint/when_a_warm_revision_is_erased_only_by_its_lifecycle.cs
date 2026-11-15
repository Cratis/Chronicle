// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.EventSequences.Idempotency.for_AppendFingerprint;

public class when_a_warm_revision_is_erased_only_by_its_lifecycle : given.a_stored_protected_intent
{
    async Task Establish()
    {
        await _backing.SaveFor("store", "ns", "subject", _key, 999u);
        var keys = Substitute.For<IEncryptionKeyStorage>();
        keys.TryGetAtRevisionFor("store", "ns", "subject", 999u).Returns(_ => _backing.TryGetAtRevisionFor("store", "ns", "subject", 999u));
        _cache = new(keys);
        await _cache.TryGetAtRevisionFor("store", "ns", "subject", 999u);
        _original = _original with { ProtectedParts = [_original.ProtectedParts[0] with { Revision = 999u }] };
        keys.GetErasureForRelease("store", "ns", "subject").Returns(new EncryptionKeyErasure(1u, [], false, 2, ErasedLifecycle: 1));
    }

    async Task Because() => _comparison = await Compare();

    [Fact] void should_preserve_the_cached_staged_lifecycle() => _comparison.Status.ShouldEqual(FingerprintComparisonStatus.VerifiedAfterErasure);
}
