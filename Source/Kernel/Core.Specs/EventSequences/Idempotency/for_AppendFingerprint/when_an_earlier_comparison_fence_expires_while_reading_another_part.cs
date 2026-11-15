// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.EventSequences.Idempotency.for_AppendFingerprint;

public class when_an_earlier_comparison_fence_expires_while_reading_another_part : given.a_stored_protected_intent
{
    readonly TaskCompletionSource _paused = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _resume = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly ProtectedValues.for_ReleaseKeys.given.a_release_clock _clock = new();
    bool _erased;
    int _subjectReads;

    void Establish()
    {
        _input.Content["secret"] = "classified";
        _input = _input with { ProtectedValues = [.. _input.ProtectedValues, new("secret", ProtectionKind.EncryptedNamespace, "namespace", JsonValue.Create("classified"))] };
        var namespaceUse = new ProtectedValueUse("secret", ProtectionKind.EncryptedNamespace, JsonValue.Create("classified"), new(ProtectionKind.EncryptedNamespace, "namespace", _key, 1u));
        _original = Compute(_input, Use("name", ProtectionKind.Pii, _key), namespaceUse);
        var keys = Substitute.For<IEncryptionKeyStorage>();
        keys.TryGetAtRevisionFor("store", "ns", Arg.Any<EncryptionKeyIdentifier>(), 1u).Returns(new EncryptionKeyAtRevision(_key, 1u, 1));
        keys.GetErasureForRelease("store", "ns", Arg.Any<EncryptionKeyIdentifier>()).Returns(call => ReadFence(call.ArgAt<EncryptionKeyIdentifier>(2)));
        _cache = new(keys);
    }

    async Task Because()
    {
        using var scope = ReleaseKeys.BeginScope(_clock);
        var comparison = Compare();
        await _paused.Task;
        _erased = true;
        _clock.Advance(TimeSpan.FromSeconds(6));
        _resume.SetResult();
        _comparison = await comparison;
    }

    [Fact] void should_refresh_the_earlier_fence_at_settlement() => _subjectReads.ShouldEqual(2);
    [Fact] void should_report_the_revoked_earlier_part() => _comparison.Status.ShouldEqual(FingerprintComparisonStatus.VerifiedAfterErasure);

    async Task<EncryptionKeyErasure?> ReadFence(EncryptionKeyIdentifier identifier)
    {
        if (identifier.Value == "subject")
        {
            _subjectReads++;
            return _erased ? new(1u, [], false, 2, ErasedLifecycle: 1) : null;
        }

        if (!_erased)
        {
            _paused.SetResult();
            await _resume.Task;
        }

        return null;
    }
}
