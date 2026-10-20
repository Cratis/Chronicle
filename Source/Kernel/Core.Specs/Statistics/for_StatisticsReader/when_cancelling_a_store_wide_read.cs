// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Contracts.ReadModels;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Statistics.for_StatisticsReader;

public class when_cancelling_a_store_wide_read : given.a_statistics_reader
{
    readonly CancellationTokenSource _cancellation = new();
    readonly TaskCompletionSource _full = new(TaskCreationOptions.RunContinuationsAsynchronously);
    int _started;
    readonly ConcurrentBag<CallContext> _contexts = [];
    Exception _error;

    void Establish()
    {
        _namespaces.GetAll().Returns(Enumerable.Range(0, 24).Select(index => (Concepts.EventStoreNamespaceName)$"namespace-{index}"));
        _readModels.GetInstances(Arg.Any<GetInstancesRequest>(), Arg.Any<CallContext>()).Returns(async call =>
        {
            if (Interlocked.Increment(ref _started) == StatisticsReader.MaximumConcurrentNamespaceReads) _full.TrySetResult();
            var context = call.Arg<CallContext>();
            _contexts.Add(context);
            await Task.Delay(Timeout.InfiniteTimeSpan, context.CancellationToken);
            return new GetInstancesResponse();
        });
    }

    async Task Because()
    {
        IStatisticsReader reader = _reader;
        var read = reader.GetEventTypeStatisticsForEventStore("store", _cancellation.Token);
        try
        {
            await _full.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }
        finally
        {
            await _cancellation.CancelAsync();
        }
        _error = await Catch.Exception(async () => await read);
    }

    [Fact] void should_cancel_the_request() => (_error is OperationCanceledException).ShouldBeTrue();
    [Fact] void should_not_schedule_the_remaining_namespaces() => _started.ShouldEqual(StatisticsReader.MaximumConcurrentNamespaceReads);
    [Fact] void should_forward_cancellation_to_the_contributing_reads() => _contexts.Count(context => context.CancellationToken.IsCancellationRequested).ShouldEqual(StatisticsReader.MaximumConcurrentNamespaceReads);

    void Destroy() => _cancellation.Dispose();
}
