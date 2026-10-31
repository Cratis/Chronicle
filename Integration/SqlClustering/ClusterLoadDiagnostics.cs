// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reactors;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Integration.SqlClustering;

internal static class ClusterLoadDiagnostics
{
    const int SequenceLimit = 50;

    internal static ILoggerFactory CreateClientLoggerFactory(TextWriter output) => LoggerFactory.Create(builder => builder
        .AddFilter((category, level) => category == "Cratis.Chronicle.Reactors.Reactors" && level >= LogLevel.Information)
        .AddProvider(new ClientDiagnosticsProvider(TextWriter.Synchronized(output))));

    internal sealed class MembershipDiagnostics
    {
        string? _previous;
        int _written;

        public string? Record(IReadOnlyList<MembershipEntry> entries)
        {
            var snapshot = $"membership entries={entries.Count}: {string.Join(", ", entries.OrderBy(entry => entry.Address).ThenBy(entry => entry.Generation).Take(12).Select(entry => $"{entry.Address}@{entry.Generation} status={entry.Status}"))}";
            if (snapshot == _previous || _written >= 40)
            {
                return null;
            }

            _previous = snapshot;
            _written++;
            return $"{DateTimeOffset.UtcNow:O} SQL {snapshot}";
        }
    }

    sealed class ClientDiagnosticsProvider(TextWriter output) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new ClientDiagnosticsLogger(output);

        public void Dispose() { }
    }

    sealed class ClientDiagnosticsLogger(TextWriter output) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            // Only these lifecycle templates are safe: never emit general connection/options or exception logs.
            if (eventId.Id is 8100 or 8101)
            {
                output.WriteLine($"{DateTimeOffset.UtcNow:O} SQL client {formatter(state, null).Replace('\r', ' ').Replace('\n', ' ')}");
            }
        }
    }

    public static async Task Dump(
        IReactorHandler reactor,
        IEnumerable<(Guid Id, EventSequenceNumber Sequence)> stored,
        IEnumerable<(Guid Id, EventSequenceNumber Sequence)> observed,
        TextWriter output)
    {
        var storedEvents = stored.ToArray();
        var observedEvents = observed.ToHashSet();
        await output.WriteLineAsync($"Observer {reactor.Id}: {DescribeMissingSequences(storedEvents, observedEvents)}");

        // This surface has no Occurred or acknowledgment timestamps. Preserve the stored input's order as a
        // correlation aid without pretending it is the order in which concurrent append responses arrived.
        var missingPositions = storedEvents.Select((@event, index) => (@event, index))
            .Where(item => !observedEvents.Contains(item.@event))
            .OrderBy(item => item.@event.Sequence.Value)
            .Take(SequenceLimit)
            .Select(item => $"{item.@event.Sequence.Value}@{item.index}");
        await output.WriteLineAsync($"Observer {reactor.Id}: missing sequence@stored-order-index (zero-based, first {SequenceLimit}): {string.Join(", ", missingPositions)}");
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
