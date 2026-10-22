// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;
using Grpc.Core;

namespace Cratis.Chronicle.Integration.SqlClustering;

public sealed partial class ClusterLoad : IAsyncDisposable
{
    readonly ConcurrentQueue<(Guid Id, EventSequenceNumber Sequence)> _observed = new();
    readonly ConcurrentQueue<(Guid Id, EventSequenceNumber Sequence)> _acknowledged = new();
    readonly ConcurrentQueue<Guid> _unknown = new();
    readonly ConcurrentQueue<Task> _outstanding = new();
    readonly ConcurrentDictionary<(string Address, int Generation), bool> _departedSilos = new();
    readonly CancellationTokenSource _stop = new();
    readonly string _source = Guid.NewGuid().ToString();
    ChronicleClient _client = null!;
    IEventStore _store = null!;
    IReactorHandler _reactor = null!;
    Task _load = Task.CompletedTask;
    int _failures;

    public int Successes => _acknowledged.Count;

    /// <summary>
    /// Records that the fixture is about to kill or stop the given silo incarnation, so an append forwarded to it
    /// while the cluster has not yet noticed can be told apart from any other connection failure.
    /// </summary>
    /// <param name="silo">The membership entry of the silo incarnation that is going away.</param>
    public void SiloDeparting(MembershipEntry silo) => _departedSilos[(silo.Address, silo.Generation)] = true;

    public async Task Start(SqlCluster cluster)
    {
        var options = new ChronicleOptions(cluster.ConnectionString) { ConnectTimeout = 30 };
        options.ExplicitArtifacts.RegisterEventType<ClusterEventAppended>();
        _client = new(options);

        // Automatic registration is what re-registers the reactor after the client reconnects to another node, the
        // way an application does; a manually registered store stays unregistered once its node goes away.
        _store = await _client.GetEventStore("sql-clustering");
        _reactor = await _store.Reactors.Register(
            "cluster-integrity",
            builder => builder.WithEventType(_store.EventTypes.GetEventTypeFor(typeof(ClusterEventAppended))),
            (@event, _) =>
            {
                _observed.Enqueue((@event.Content[nameof(ClusterEventAppended.Id)]!.GetValue<Guid>(), @event.Context.SequenceNumber));
                return Task.CompletedTask;
            });
        await _reactor.WaitTillActive(TimeSpan.FromSeconds(60));
        _load = Run();
    }

    public async Task WaitForProgress(int minimum)
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(90));
        using var poll = new PeriodicTimer(TimeSpan.FromMilliseconds(100));
        while (Successes < minimum)
        {
            if (_load.IsCompleted)
            {
                await _load;
                throw new InvalidOperationException("Append load ended before making progress.");
            }

            await poll.WaitForNextTickAsync(deadline.Token);
        }
    }

    public async Task StopAndVerify()
    {
        await _stop.CancelAsync();
        await _load;
        await SettleOutstandingAppends();
        var acknowledged = _acknowledged.ToArray();
        (acknowledged.Length >= 20).ShouldBeTrue();

        // An attempt that timed out or lost its connection has an unknown outcome: it may or may not have been stored.
        // Such attempts are allowed in the log at most once, but every acknowledged append must be there exactly once.
        var allowed = acknowledged.Select(append => append.Id).Concat(_unknown).ToHashSet();
        var stored = await _store.EventLog.GetForEventSourceIdAndEventTypes(_source, []);
        await _reactor.WaitTillReachesEventSequenceNumber(stored.Max(@event => @event.Context.SequenceNumber), TimeSpan.FromSeconds(90));
        var storedIds = stored.Select(@event => ((ClusterEventAppended)@event.Content).Id).ToArray();
        var storedSequences = stored.Select(@event => @event.Context.SequenceNumber.Value).Order().ToArray();
        Console.WriteLine($"Acknowledged={acknowledged.Length}, failed={_failures}, unknown={_unknown.Count}, stored={stored.Count}, observed={_observed.Count}");
        storedIds.Distinct().Count().ShouldEqual(storedIds.Length);
        storedIds.All(allowed.Contains).ShouldBeTrue();
        acknowledged.All(append => storedIds.Contains(append.Id)).ShouldBeTrue();
        acknowledged.All(append => stored.Any(@event => @event.Context.SequenceNumber == append.Sequence && ((ClusterEventAppended)@event.Content).Id == append.Id)).ShouldBeTrue();
        storedSequences.Zip(storedSequences.Skip(1), (first, next) => next == first + 1).All(contiguous => contiguous).ShouldBeTrue();
        VerifyObservations(stored.Select(@event => (((ClusterEventAppended)@event.Content).Id, @event.Context.SequenceNumber)), _observed);
    }

    public async ValueTask DisposeAsync()
    {
        await _stop.CancelAsync();
        try
        {
            await _load;
        }
        finally
        {
            _client?.Dispose();
            _stop.Dispose();
        }
    }

    internal static void VerifyObservations(IEnumerable<(Guid Id, EventSequenceNumber Sequence)> stored, IEnumerable<(Guid Id, EventSequenceNumber Sequence)> observed)
    {
        // Delivery is at-least-once. Repeated handling must refer to the same stored event, not just the same payload id.
        // This fixture has one reactor, sequence and partition, so the sequence number also fixes the ReactorDelivery identity.
        var storedEvents = stored.ToHashSet();
        var observedEvents = observed.ToHashSet();
        storedEvents.All(observedEvents.Contains).ShouldBeTrue();
        observedEvents.All(storedEvents.Contains).ShouldBeTrue();
    }

    internal static bool IsUncertainAppend(Exception exception) =>
        exception is RpcException or TimeoutException or ConnectionUnavailable or ConnectionTimedOut ||
        IsUnavailableSiloAppend(exception.GetType().FullName, exception.Message);

    // Until the cluster has declared a killed or stopped silo dead, a surviving silo can still forward an append to
    // it and fail to connect. Like an append to an unavailable silo (#5231), the outcome is uncertain, so it is
    // recorded as unknown and still verified against the stream (#5127). Only the incarnations this fixture took
    // down qualify; a refused connection to any other silo is a real failure.
    internal static bool IsAppendToDepartedSilo(string? exceptionType, string message, ICollection<(string Address, int Generation)> departed)
    {
        if (exceptionType != "Cratis.Chronicle.Contracts.Commands.CommandFailed" ||
            !message.StartsWith("Command failed: Exception while sending message: Orleans.Runtime.Messaging.ConnectionFailedException: Unable to connect to endpoint S", StringComparison.Ordinal) ||
            !message.Contains("ConnectionRefused", StringComparison.Ordinal))
        {
            return false;
        }

        var endpoint = DepartedEndpoint().Match(message);
        return endpoint.Success &&
            departed.Contains((endpoint.Groups["address"].Value, int.Parse(endpoint.Groups["generation"].Value, System.Globalization.CultureInfo.InvariantCulture)));
    }

    internal static bool IsUnavailableSiloAppend(string? exceptionType, string message)
    {
        // CommandFailed belongs to the client's private Contracts dependency; do not add a public dependency on it.
        return exceptionType == "Cratis.Chronicle.Contracts.Commands.CommandFailed" &&
            message.StartsWith("Command failed: The target silo became unavailable for message: Request ", StringComparison.Ordinal) &&
            message.Contains(" Cratis.Chronicle.EventSequences.IEventSequence.Append(", StringComparison.Ordinal);
    }

    async Task Run()
    {
        // Offered load is deliberately paced, not a readiness sleep. The harness never retries an append itself;
        // the client keeps its production defaults (including gRPC retries on Unavailable), so a failure counted
        // here is one an application using Chronicle would actually see.
        using var cadence = new PeriodicTimer(TimeSpan.FromMilliseconds(100));
        while (!_stop.IsCancellationRequested)
        {
            var id = Guid.NewGuid();
            try
            {
                var append = _store.EventLog.Append(_source, new ClusterEventAppended(id));
                _outstanding.Enqueue(append);
                var result = await append.WaitAsync(TimeSpan.FromSeconds(30));
                if (result.IsSuccess)
                {
                    _acknowledged.Enqueue((id, result.SequenceNumber));
                }
                else
                {
                    Failed($"{id}: {string.Join(';', result.Errors)}");
                }
            }
            catch (Exception exception) when (IsUncertainAppend(exception) || IsAppendToDepartedSilo(exception.GetType().FullName, exception.Message, _departedSilos.Keys))
            {
                // The request may still complete or may have been stored before the connection broke.
                // An in-flight append to a dead silo has the same uncertain outcome:
                // https://github.com/Cratis/Chronicle/issues/5231.
                _unknown.Enqueue(id);
                Failed($"{id}: {exception.GetType().Name}: {exception.Message}");
            }

            if (!_stop.IsCancellationRequested)
            {
                await cadence.WaitForNextTickAsync();
            }
        }
    }

    async Task SettleOutstandingAppends()
    {
        // An append abandoned by WaitAsync keeps running; the log is only read once every attempt has finished,
        // so a late append can neither escape verification nor race the reactor check.
        // Their outcome is already recorded as unknown; only their completion matters here, not their result.
        var outstanding = Task.WhenAll(_outstanding.Select(append => append.ContinueWith(_ => { }, TaskScheduler.Default)));
        try
        {
            await outstanding.WaitAsync(TimeSpan.FromMinutes(2));
        }
        catch (TimeoutException)
        {
            throw new InvalidOperationException("Appends were still outstanding two minutes after the load stopped.");
        }
    }

    void Failed(string reason)
    {
        Console.WriteLine($"Failed append: {reason}");
        if (++_failures > 30)
        {
            throw new InvalidOperationException("More than 30 append attempts failed during the membership transition.");
        }
    }

    [GeneratedRegex(@"Unable to connect to endpoint S(?<address>[^:\s]+):\d+:(?<generation>-?\d+)\.", RegexOptions.CultureInvariant)]
    private static partial Regex DepartedEndpoint();
}
