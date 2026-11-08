// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.AzureKeyVault;
using Cratis.Chronicle.Storage.AzureKeyVault.for_EncryptionKeyStorageContract.given;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.EventSequences.Idempotency.for_AppendFingerprint;

public class when_a_warm_azure_revision_is_erased_only_by_its_lifecycle : given.a_stored_protected_intent
{
    readonly FenceAuthority _authority = new();
    readonly TaskCompletionSource _paused = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _resume = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly ProtectedValues.for_ReleaseKeys.given.a_release_clock _clock = new();
    EncryptionKeyIdentifier _lastAuthority;
    EncryptionKeyIdentifier _subjectAuthority;
    EncryptionKeyIdentifier _namespaceAuthority;
    bool _comparing;
    bool _erased;
    int _subjectReads;

    async Task Establish()
    {
        var fence = Substitute.For<IEncryptionKeyFence>();
        fence.IsDurable.Returns(true);
        fence.ReadFor("store", "ns", Arg.Any<EncryptionKeyIdentifier>()).Returns(call => ReadFence(call.ArgAt<EncryptionKeyIdentifier>(2)));
        fence.CompareExchangeFor("store", "ns", Arg.Any<EncryptionKeyIdentifier>(), Arg.Any<EncryptionKeyFenceState>(), Arg.Any<EncryptionKeyFenceState>())
            .Returns(call => _authority.CompareExchangeFor("store", "ns", call.ArgAt<EncryptionKeyIdentifier>(2), call.ArgAt<EncryptionKeyFenceState>(3), call.ArgAt<EncryptionKeyFenceState>(4)));
        fence.PublishRevision("store", "ns", Arg.Any<EncryptionKeyIdentifier>(), Arg.Any<EncryptionKeyFenceState>(), Arg.Any<EncryptionKeyPublication>())
            .Returns(call => ((IEncryptionKeyFence)_authority).PublishRevision("store", "ns", call.ArgAt<EncryptionKeyIdentifier>(2), call.ArgAt<EncryptionKeyFenceState>(3), call.ArgAt<EncryptionKeyPublication>(4)));
        var azure = new AzureKeyVaultEncryptionKeyStorage(new VersionedSecretClient(), null, fence, _ => Task.CompletedTask);
        await azure.SaveFor("store", "ns", "subject", _key, 999u);
        _subjectAuthority = _lastAuthority;
        await azure.SaveFor("store", "ns", "namespace", _key, 999u);
        _namespaceAuthority = _lastAuthority;
        _cache = new(azure);
        await _cache.TryGetAtRevisionFor("store", "ns", "subject", 999u);
        await _cache.TryGetAtRevisionFor("store", "ns", "namespace", 999u);

        _input.Content["secret"] = "classified";
        _input = _input with { ProtectedValues = [.. _input.ProtectedValues, new("secret", ProtectionKind.EncryptedNamespace, "namespace", JsonValue.Create("classified"))] };
        var subject = new ProtectedValueUse("name", ProtectionKind.Pii, JsonValue.Create("Alice"), new(ProtectionKind.Pii, "subject", _key, 999u));
        var shared = new ProtectedValueUse("secret", ProtectionKind.EncryptedNamespace, JsonValue.Create("classified"), new(ProtectionKind.EncryptedNamespace, "namespace", _key, 999u));
        _original = Compute(_input, subject, shared);
        _input.Content["name"] = "Alice";
    }

    async Task Because()
    {
        using var scope = ReleaseKeys.BeginScope(_clock);
        _comparing = true;
        var comparison = Compare();
        await Task.WhenAny(_paused.Task, comparison);
        if (comparison.IsCompleted)
        {
            _comparison = await comparison;
            return;
        }
        var observed = await _authority.ReadFor("store", "ns", _subjectAuthority);
        var next = observed with
        {
            Erasure = observed.Erasure with { Version = observed.Erasure.Version + 1, ErasedThrough = 1u, ErasedLifecycle = observed.Erasure.Lifecycle, NewKeyAllowed = false }
        };
        (await _authority.CompareExchangeFor("store", "ns", _subjectAuthority, observed, next)).ShouldBeTrue();
        _erased = true;
        _clock.Advance(TimeSpan.FromSeconds(6));
        _resume.SetResult();
        _comparison = await comparison;
    }

    [Fact] void should_refresh_the_earlier_azure_fence_at_settlement() => _subjectReads.ShouldEqual(2);
    [Fact] void should_report_the_erased_warm_pair() => _comparison.Status.ShouldEqual(FingerprintComparisonStatus.VerifiedAfterErasure);

    async Task<EncryptionKeyFenceState> ReadFence(EncryptionKeyIdentifier identifier)
    {
        _lastAuthority = identifier;
        if (_comparing && identifier == _subjectAuthority) _subjectReads++;
        if (_comparing && identifier == _namespaceAuthority && !_erased)
        {
            _paused.SetResult();
            await _resume.Task;
        }
        return await _authority.ReadFor("store", "ns", identifier);
    }
}
