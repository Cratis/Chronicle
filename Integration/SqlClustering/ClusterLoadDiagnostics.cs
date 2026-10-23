// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Integration.SqlClustering;

internal static class ClusterLoadDiagnostics
{
    const int SequenceLimit = 50;

    public static async Task Dump(
        IReactorHandler reactor,
        IEnumerable<(Guid Id, EventSequenceNumber Sequence)> stored,
        IEnumerable<(Guid Id, EventSequenceNumber Sequence)> observed,
        TextWriter output)
    {
        await output.WriteLineAsync($"Observer {reactor.Id}: {DescribeMissingSequences(stored, observed)}");
        await DumpState(reactor, output);
        await DumpFailedPartitions(reactor, output);
    }

    internal static string DescribeMissingSequences(
        IEnumerable<(Guid Id, EventSequenceNumber Sequence)> stored,
        IEnumerable<(Guid Id, EventSequenceNumber Sequence)> observed)
    {
        var missing = stored.Except(observed).Select(@event => @event.Sequence.Value).Order().ToArray();
        return $"missing stored event sequence numbers ({missing.Length}, first {SequenceLimit}): {string.Join(", ", missing.Take(SequenceLimit))}";
    }

    internal static string DescribeState(ReactorState state) =>
        $"Observer {state.Id}: RunningState={state.RunningState}, IsSubscribed={state.IsSubscribed}, LastHandledEventSequenceNumber={state.LastHandledEventSequenceNumber}, NextEventSequenceNumber={state.NextEventSequenceNumber}, TailEventSequenceNumber={state.TailEventSequenceNumber}";

    internal static string DescribeFailedPartition(FailedPartition partition)
    {
        var last = partition.Attempts.LastOrDefault();
        return $"Failed partition {partition.Partition}: ObserverId={partition.ObserverId}, IsResolved={partition.IsResolved}, IsQuarantined={partition.IsQuarantined}, LastFailedSequenceNumber={last?.SequenceNumber}, Kind={last?.Kind}, Messages={string.Join("; ", last?.Messages ?? [])}";
    }

    static async Task DumpState(IReactorHandler reactor, TextWriter output)
    {
        try
        {
            var state = await reactor.GetState().WaitAsync(TimeSpan.FromSeconds(10));
            await output.WriteLineAsync(DescribeState(state));
        }
        catch (Exception exception)
        {
            await output.WriteLineAsync($"Observer {reactor.Id} state unavailable: {exception}");
        }
    }

    static async Task DumpFailedPartitions(IReactorHandler reactor, TextWriter output)
    {
        try
        {
            var partitions = (await reactor.GetFailedPartitions().WaitAsync(TimeSpan.FromSeconds(10))).ToArray();
            await output.WriteLineAsync($"Observer {reactor.Id}: failed partitions={partitions.Length}");
            foreach (var partition in partitions)
            {
                await output.WriteLineAsync(DescribeFailedPartition(partition));
            }
        }
        catch (Exception exception)
        {
            await output.WriteLineAsync($"Observer {reactor.Id} failed partitions unavailable: {exception}");
        }
    }
}
