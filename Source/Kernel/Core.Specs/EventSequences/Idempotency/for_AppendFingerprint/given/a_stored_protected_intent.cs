// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.EventSequences.Idempotency.for_AppendFingerprint.given;

public class a_stored_protected_intent : an_append_intent
{
    protected InMemoryEncryptionKeyStorage _backing;
    protected CacheEncryptionKeyStorage _cache;
    internal FingerprintComparisonResult _comparison;
    readonly controlled_time_provider _clock = new();

    async Task Establish()
    {
        _backing = new();
        _cache = new(_backing, _clock);
        await _backing.SaveFor("store", "ns", "subject", _key, EncryptionKeyRevision.Initial);
        Protect();
    }

    internal async Task<FingerprintComparisonResult> Compare()
    {
        var result = await AppendFingerprintComparison.Compare(_original, _input, _cache, _cache, "store", "ns");
        result.TryGetResult(out var comparison).ShouldBeTrue();
        return comparison;
    }

    protected async Task Erase()
    {
        await _cache.RecordErasureFor("store", "ns", "subject");
        await _cache.DeleteFor("store", "ns", "subject");
    }

    internal async Task NewLifecycle(bool rememberAbsent = false)
    {
        await Erase();
        await _cache.AllowNewKeyFor("store", "ns", "subject");
        if (rememberAbsent)
        {
            (await _cache.TryGetFor("store", "ns", "subject", 2)).ShouldBeNull();
        }

        var pair = await _backing.GetOrAddWithRevisionFor("store", "ns", "subject", new([7, 8, 9], [10, 11, 12]));
        _clock.Current += TimeSpan.FromSeconds(1);
        _key = pair.Key;
        _original = Compute(_input, Use("name", ProtectionKind.Pii, pair.Key, pair.Revision));
    }

    internal void ChangeProtectedValue()
    {
        _input.Content["name"] = "Bob";
        _input = _input with { ProtectedValues = [new("name", ProtectionKind.Pii, "subject", _input.Content["name"]!.DeepClone())] };
    }

    sealed class controlled_time_provider : TimeProvider
    {
        internal DateTimeOffset Current { get; set; } = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Current;
    }
}
