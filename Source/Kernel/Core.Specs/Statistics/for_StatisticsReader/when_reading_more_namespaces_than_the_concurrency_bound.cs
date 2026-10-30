// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Statistics.for_StatisticsReader;

public class when_reading_more_namespaces_than_the_concurrency_bound : given.a_statistics_reader
{
    readonly TaskCompletionSource _full = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    int _active;
    int _maximum;
    IEnumerable<EventTypeStatistics> _result;

    void Establish()
    {
        _namespaces.GetAll().Returns(Enumerable.Range(0, 24).Select(index => (Concepts.EventStoreNamespaceName)$"namespace-{index}"));
        _readModels.GetInstances(Arg.Any<GetInstancesRequest>(), Arg.Any<CallContext>()).Returns(async call =>
        {
            var active = Interlocked.Increment(ref _active);
            RecordMaximum(active);
            if (active == StatisticsReader.MaximumConcurrentNamespaceReads) _full.TrySetResult();
            try
            {
                await _release.Task;
                return new GetInstancesResponse { Instances = ["{\"eventType\":\"shared\",\"count\":1}"] };
            }
            finally
            {
                Interlocked.Decrement(ref _active);
            }
        });
    }

    async Task Because()
    {
        var read = _reader.GetEventTypeStatisticsForEventStore("store");
        try
        {
            await _full.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }
        finally
        {
            _release.TrySetResult();
        }
        _result = await read;
    }

    [Fact] void should_use_but_never_exceed_the_bound() => _maximum.ShouldEqual(StatisticsReader.MaximumConcurrentNamespaceReads);
    [Fact] void should_read_every_namespace() => _result.Count().ShouldEqual(24);
    [Fact] void should_preserve_the_total() => _result.Sum(row => row.Count).ShouldEqual(24);

    void RecordMaximum(int active)
    {
        int previous;
        do
        {
            previous = Volatile.Read(ref _maximum);
            if (previous >= active) return;
        }
        while (Interlocked.CompareExchange(ref _maximum, active, previous) != previous);
    }
}
