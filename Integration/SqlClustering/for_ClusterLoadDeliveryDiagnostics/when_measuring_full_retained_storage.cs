// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.SqlClustering.for_ClusterLoadDeliveryDiagnostics;

public class when_measuring_full_retained_storage : Specification
{
    ClusterLoadDeliveryDiagnostics _diagnostics = null!;
    long _allocated;

    void Because()
    {
        var before = GC.GetAllocatedBytesForCurrentThread();
        _diagnostics = new();
        var stream = Guid.NewGuid();
        var id = Guid.NewGuid();
        _diagnostics.Record(new(1, stream));
        for (ulong sequence = 0; sequence < 16384; sequence++)
        {
            var batch = (long)sequence + 1;
            _diagnostics.Record(new(5, stream, batch, Count: 1));
            _diagnostics.Record(new(6, stream, batch, Sequence: sequence));
            _diagnostics.Callback(id, sequence, () => { });
        }
        _diagnostics.Describe([], []);
        _allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Console.WriteLine($"Full bounded recorder: allocated including indexes and one report={_allocated}; conservative retained-storage bound={_diagnostics.StorageUpperBound}; recent-record-bytes={_diagnostics.RecentRecordBytes}.");
    }

    void Destroy() => _diagnostics.Dispose();

    [Fact] public void should_fill_every_sequence_slot() => _diagnostics.SequenceCount.ShouldEqual(16384);
    [Fact] public void should_measure_less_than_16_mib_even_including_report_allocations() => (_allocated <= 16 * 1024 * 1024).ShouldBeTrue();
    [Fact] public void should_fit_the_conservative_storage_bound() => (_allocated <= _diagnostics.StorageUpperBound).ShouldBeTrue();
    [Fact] public void should_bound_each_recent_record() => (_diagnostics.RecentRecordBytes <= 128).ShouldBeTrue();
}
