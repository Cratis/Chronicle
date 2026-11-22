// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.EventSequences.Idempotency.for_AppendFingerprint;

public class when_comparing_metadata_free_intent : given.an_append_intent
{
    const int Batches = 10;
    const int CallsPerBatch = 1_000;

    const int PlainAllocationCap = 4096;
#if DEBUG
    /// <summary>
    /// Debug also allocates the async state machine; each overhead cap leaves 16 bytes/call of headroom.
    /// </summary>
    const int AdditionalAllocationCap = 432;
#else
    const int AdditionalAllocationCap = 208;
#endif

    readonly EventStoreName _store = "store";
    readonly EventStoreNamespaceName _namespace = "ns";
    readonly IEncryptionKeyStorage _keys = Substitute.For<IEncryptionKeyStorage>();
    readonly IEvictEncryptionKeyCache _cache = Substitute.For<IEvictEncryptionKeyCache>();
    FingerprintComparisonResult _comparison;
    double _plainBytesPerCall;
    double _comparisonBytesPerCall;

    void Because()
    {
        MeasureSteadyStateAllocation(() => AppendFingerprint.Plain(_input), () => Compare());

        // Metadata-free retries must not complete an unrelated ambient release's observations.
        using var scope = ReleaseKeys.BeginScope();
        ReleaseKeys.ObserveForCompletion(_keys, (_store, _namespace, "unrelated"), _ => throw new InvalidOperationException("Unrelated release completed"));
        _comparison = Compare();
    }

    [Fact] void should_verify_the_retry() => _comparison.Status.ShouldEqual(FingerprintComparisonStatus.Verified);
    [Fact] void should_not_read_keys_or_fences() => _keys.ReceivedCalls().ShouldBeEmpty();
    [Fact]
    void should_allocate_only_the_result_and_completed_task()
    {
        var additionalBytes = _comparisonBytesPerCall - _plainBytesPerCall;
        var measurement = $"Minimum allocation per call: plain {_plainBytesPerCall} bytes (sanity cap: {PlainAllocationCap}); comparison {_comparisonBytesPerCall} bytes; additional {additionalBytes} bytes (cap: {AdditionalAllocationCap}).";
        TestContext.Current.TestOutputHelper!.WriteLine(measurement);
        Assert.True(_plainBytesPerCall <= PlainAllocationCap && additionalBytes <= AdditionalAllocationCap, measurement);
    }

    void MeasureSteadyStateAllocation(Action plain, Action compare)
    {
        // Warm both paths through tiered JIT before interleaved sampling.
        for (var index = 0; index < 10_000; index++)
        {
            plain();
            compare();
        }

        var minimumPlainBytes = long.MaxValue;
        var minimumComparisonBytes = long.MaxValue;
        for (var batch = 0; batch < Batches; batch++)
        {
            var plainBytes = 0L;
            var comparisonBytes = 0L;
            for (var index = 0; index < CallsPerBatch; index++)
            {
                plainBytes += MeasureAllocation(plain);
                comparisonBytes += MeasureAllocation(compare);
            }

            // Independent minima prevent a noisy plain sample from masking comparison overhead.
            minimumPlainBytes = Math.Min(minimumPlainBytes, plainBytes);
            minimumComparisonBytes = Math.Min(minimumComparisonBytes, comparisonBytes);
        }

        _plainBytesPerCall = minimumPlainBytes / (double)CallsPerBatch;
        _comparisonBytesPerCall = minimumComparisonBytes / (double)CallsPerBatch;
    }

    static long MeasureAllocation(Action action)
    {
        var before = GC.GetAllocatedBytesForCurrentThread();
        action();
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    FingerprintComparisonResult Compare()
    {
        var comparisonTask = AppendFingerprintComparison.Compare(_original, _input, _keys, _cache, _store, _namespace);
        if (!comparisonTask.IsCompletedSuccessfully) throw new InvalidOperationException("The metadata-free comparison did not complete synchronously.");
        var result = comparisonTask.GetAwaiter().GetResult();
        result.TryGetResult(out var comparison);
        return comparison;
    }
}
