// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.EventSequences.Idempotency.for_AppendFingerprint;

public class when_comparing_metadata_free_intent : given.an_append_intent
{
    readonly IEncryptionKeyStorage _keys = Substitute.For<IEncryptionKeyStorage>();
    readonly IEvictEncryptionKeyCache _cache = Substitute.For<IEvictEncryptionKeyCache>();
    FingerprintComparisonResult _comparison;
    long _additionalBytes;

    void Because()
    {
        // Warm both paths before measuring only the comparison overhead over the required plain digest.
        for (var index = 0; index < 100; index++)
        {
            AppendFingerprint.Plain(_input);
            Compare();
        }

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 1000; index++) AppendFingerprint.Plain(_input);
        var plainBytes = GC.GetAllocatedBytesForCurrentThread() - before;
        before = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 1000; index++) Compare();
        _additionalBytes = (GC.GetAllocatedBytesForCurrentThread() - before - plainBytes) / 1000;

        // Metadata-free retries must not complete an unrelated ambient release's observations.
        using var scope = ReleaseKeys.BeginScope();
        ReleaseKeys.ObserveForCompletion(_keys, ("store", "ns", "unrelated"), _ => throw new InvalidOperationException("Unrelated release completed"));
        _comparison = Compare();
    }

    [Fact] void should_verify_the_retry() => _comparison.Status.ShouldEqual(FingerprintComparisonStatus.Verified);
    [Fact] void should_not_read_keys_or_fences() => _keys.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_allocate_only_the_result_and_completed_task() => (_additionalBytes <= 256).ShouldBeTrue();

    FingerprintComparisonResult Compare()
    {
        var result = AppendFingerprintComparison.Compare(_original, _input, _keys, _cache, "store", "ns").GetAwaiter().GetResult();
        result.TryGetResult(out var comparison);
        return comparison;
    }
}
