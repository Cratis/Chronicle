// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Reducers;
using Cratis.Chronicle.Sinks;
using Cratis.Traces;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Integration.SqlClustering;

/// <summary>
/// Appends a steady stream of events to a cluster while its membership changes, and verifies none was lost or doubled.
/// Uncertain appends to a departed incarnation are exempt from the attempt budget for 90 seconds after departure.
/// </summary>
/// <param name="transitions">
/// How many nodes the scenario takes down. The attempt budget grows with each transition. Returned failures and
/// other uncertain appends count immediately; departed-incarnation appends count only after the departure window.
/// </param>
/// <param name="timeProvider">The clock used to bound the departed-incarnation exemption.</param>
public sealed partial class ClusterLoad(int transitions = 1, TimeProvider? timeProvider = null) : IAsyncDisposable
{
    const int FailureBudgetPerTransition = 30;

    readonly ConcurrentQueue<(Guid Id, EventSequenceNumber Sequence)> _observed = new();
    readonly ConcurrentQueue<(Guid Id, EventSequenceNumber Sequence)> _acknowledged = new();
    readonly ConcurrentQueue<Guid> _unknown = new();
    readonly ConcurrentQueue<Task> _outstanding = new();
    readonly ConcurrentDictionary<(string Address, int Generation), long> _departedSilos = new();
    readonly CancellationTokenSource _stop = new();
    readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    readonly string _source = Guid.NewGuid().ToString();
    readonly ClusterReconnectReducer _reconnectReducer = new();
    ServiceProvider? _services;
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
    public void SiloDeparting(MembershipEntry silo) => _departedSilos[(silo.Address, silo.Generation)] = _timeProvider.GetTimestamp();

    public async Task Start(SqlCluster cluster, bool verifyReconnect = false)
    {
        var options = new ChronicleOptions(cluster.ConnectionString) { ConnectTimeout = 30, DefaultSinkTypeId = WellKnownSinkTypes.SQL };
        options.ExplicitArtifacts.RegisterEventType<ClusterEventAppended>();
        if (verifyReconnect)
        {
            options.ExplicitArtifacts.RegisterReducer<ClusterReconnectReducer>();

            // EventStore's constructor requires a keyed IActivitySource<T> for EventSequence, Reactors and
            // Reducers. A host-supplied service provider gets no graceful fallback the way the client's own
            // DefaultServiceProvider has, so this stands in for the client-side tracing registration a real
            // application host would add.
            _services = new ServiceCollection()
                .AddSingleton(_reconnectReducer)
                .AddKeyedSingleton(typeof(IActivitySource<>), ClientActivity.SourceName, typeof(FallbackActivitySource<>))
                .BuildServiceProvider();
        }
        _client = new(options, serviceProvider: _services);

        // Automatic registration is what re-registers the reactor after the client reconnects to another node, the
        // way an application does; a manually registered store stays unregistered once its node goes away.
        _store = await _client.GetEventStore(verifyReconnect ? "sql-clustering-reconnect" : "sql-clustering");
        _reactor = await _store.Reactors.Register(
            "cluster-integrity",
            builder => builder.WithEventType(_store.EventTypes.GetEventTypeFor(typeof(ClusterEventAppended))),
            (@event, _) =>
            {
                _observed.Enqueue((@event.Content[nameof(ClusterEventAppended.Id)]!.GetValue<Guid>(), @event.Context.SequenceNumber));
                return Task.CompletedTask;
            });
        await _reactor.WaitTillActive(TimeSpan.FromSeconds(60));
        if (verifyReconnect)
        {
            await _store.Reducers.GetHandlerFor(typeof(ClusterReconnectReducer)).WaitTillActive(TimeSpan.FromSeconds(60));
        }
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

    /// <summary>
    /// Requires a newly submitted, acknowledged append and delivery of that exact event to both observers.
    /// The single deadline includes append retries and observation, not just registration or active status.
    /// </summary>
    /// <param name="timeout">The complete failover and delivery budget.</param>
    /// <returns>Awaitable task.</returns>
    public async Task WaitForReconnectProgress(TimeSpan timeout)
    {
        using var deadline = new CancellationTokenSource(timeout);
        using var poll = new PeriodicTimer(TimeSpan.FromMilliseconds(100));
        while (true)
        {
            var id = Guid.NewGuid();
            var result = await TryAppend(id, deadline.Token);
            if (result is null)
            {
                await poll.WaitForNextTickAsync(deadline.Token);
                continue;
            }

            var acknowledged = (id, result.SequenceNumber);
            _acknowledged.Enqueue(acknowledged);
            while (!_observed.Contains(acknowledged) || !_reconnectReducer.HasHandled(id, result.SequenceNumber))
            {
                await poll.WaitForNextTickAsync(deadline.Token);
            }

            return;
        }
    }

    async Task<AppendResult?> TryAppend(Guid id, CancellationToken cancellationToken)
    {
        AppendResult result;
        try
        {
            var append = _store.EventLog.Append(_source, new ClusterEventAppended(id));
            _outstanding.Enqueue(append);
            result = await append.WaitAsync(cancellationToken);
        }
        catch (Exception exception) when (IsUncertainAppend(exception) || IsAppendToDepartedSilo(exception.GetType().FullName, exception.Message, _departedSilos.Keys))
        {
            RecordFailedAppend(id, exception);
            return null;
        }

        if (result.IsSuccess)
        {
            return result;
        }

        RecordFailedAppend(id, result);
        return null;
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
        var storedEvents = stored.Select(@event => (((ClusterEventAppended)@event.Content).Id, @event.Context.SequenceNumber)).ToArray();
        try
        {
            // Three restarts' worth of queued load can leave a sizeable backlog for the observer to drain; give it
            // more headroom than a single restart would need, especially on a loaded CI runner.
            await _reactor.WaitTillReachesEventSequenceNumber(stored.Max(@event => @event.Context.SequenceNumber), TimeSpan.FromSeconds(180));

            // Reaching the sequence number only proves the observer's position advanced; the handler's own
            // enqueue onto _observed can still be a few scheduler ticks behind under load. Give it a bounded
            // grace period to settle before asserting, rather than a sleep with no signal behind it.
            await WaitForObservationsToSettle(storedEvents, TimeSpan.FromSeconds(15));
            var storedIds = stored.Select(@event => ((ClusterEventAppended)@event.Content).Id).ToArray();
            var storedSequences = stored.Select(@event => @event.Context.SequenceNumber.Value).Order().ToArray();
            Console.WriteLine($"Acknowledged={acknowledged.Length}, failed={_failures}, unknown={_unknown.Count}, stored={stored.Count}, observed={_observed.Count}");
            storedIds.Distinct().Count().ShouldEqual(storedIds.Length);
            storedIds.All(allowed.Contains).ShouldBeTrue();
            acknowledged.All(append => storedIds.Contains(append.Id)).ShouldBeTrue();
            acknowledged.All(append => stored.Any(@event => @event.Context.SequenceNumber == append.Sequence && ((ClusterEventAppended)@event.Content).Id == append.Id)).ShouldBeTrue();
            storedSequences.Zip(storedSequences.Skip(1), (first, next) => next == first + 1).All(contiguous => contiguous).ShouldBeTrue();
            VerifyObservations(storedEvents, _observed);
        }
        catch
        {
            await ClusterLoadDiagnostics.Dump(_reactor, storedEvents, _observed, Console.Out);
            throw;
        }
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
            if (_services is not null)
            {
                await _services.DisposeAsync();
            }
            _stop.Dispose();
        }
    }

    async Task WaitForObservationsToSettle(IEnumerable<(Guid Id, EventSequenceNumber Sequence)> stored, TimeSpan timeout)
    {
        var storedEvents = stored.ToHashSet();
        using var deadline = new CancellationTokenSource(timeout);
        using var poll = new PeriodicTimer(TimeSpan.FromMilliseconds(100));
        while (!storedEvents.All(_observed.Contains))
        {
            try
            {
                await poll.WaitForNextTickAsync(deadline.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }
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

    internal static readonly TimeSpan DepartureWindow = TimeSpan.FromSeconds(90);
    internal static bool CountsAgainstBudget(bool departedSiloAppend, TimeSpan sinceDeparture) =>
        !departedSiloAppend || sinceDeparture > DepartureWindow;

    bool CountsAgainstBudget(Exception exception)
    {
        foreach (var departure in _departedSilos)
        {
            if (IsAppendToDepartedSilo(exception.GetType().FullName, exception.Message, [departure.Key]))
            {
                return CountsAgainstBudget(true, _timeProvider.GetElapsedTime(departure.Value));
            }
        }
        return CountsAgainstBudget(false, TimeSpan.Zero);
    }

    internal void RecordFailedAppend(Guid id, Exception exception)
    {
        _unknown.Enqueue(id);
        Failed($"{id}: {exception.GetType().Name}: {exception.Message}", CountsAgainstBudget(exception));
    }

    void RecordFailedAppend(Guid id, AppendResult result)
    {
        if (IsUncertainAppend(result))
        {
            _unknown.Enqueue(id);
        }
        Failed($"{id}: {string.Join(';', result.Errors)}");
    }

    internal static bool IsUncertainAppend(AppendResult result) =>
        !result.IsSuccess && result.Errors.Contains(AppendError.AppendOutcomeUnknown);

    internal static bool IsUncertainAppend(Exception exception) =>
        exception is RpcException or TimeoutException or ConnectionUnavailable or ConnectionTimedOut ||
        IsUnavailableSiloAppend(exception.GetType().FullName, exception.Message);

    /// <summary>
    /// Until the cluster has declared a killed or stopped silo dead, a surviving silo can still forward an append to
    /// it and fail to connect. Like an append to an unavailable silo (#5231), the outcome is uncertain, so it is
    /// recorded as unknown and still verified against the stream (#5127). Only the incarnations this fixture took
    /// down qualify; a refused connection to any other silo is a real failure.
    /// </summary>
    /// <param name="exceptionType">The full exception type name.</param>
    /// <param name="message">The connection failure message.</param>
    /// <param name="departed">The silo incarnations stopped by this fixture.</param>
    /// <returns>Whether the failure targets a silo incarnation stopped by this fixture.</returns>
    internal static bool IsAppendToDepartedSilo(string? exceptionType, string message, ICollection<(string Address, int Generation)> departed)
    {
        // Orleans reports the refused connection itself once, then - while it backs off before trying the silo
        // again - fails every send to it with "will retry after" instead. Both are the same departed silo.
        const string ConnectionFailed = "Command failed: Exception while sending message: Orleans.Runtime.Messaging.ConnectionFailedException: Unable to connect to ";
        var refused = message.StartsWith($"{ConnectionFailed}endpoint S", StringComparison.Ordinal) && message.Contains("ConnectionRefused", StringComparison.Ordinal);
        var backingOff = message.StartsWith($"{ConnectionFailed}S", StringComparison.Ordinal) && message.Contains(", will retry after ", StringComparison.Ordinal);
        if (exceptionType != "Cratis.Chronicle.Contracts.Commands.CommandFailed" || !(refused || backingOff))
        {
            return false;
        }

        var endpoint = DepartedEndpoint.Match(message);
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
                var result = await append.WaitAsync(TimeSpan.FromSeconds(30), _timeProvider);
                if (result.IsSuccess)
                {
                    _acknowledged.Enqueue((id, result.SequenceNumber));
                }
                else
                {
                    RecordFailedAppend(id, result);
                }
            }
            catch (Exception exception) when (IsUncertainAppend(exception) || IsAppendToDepartedSilo(exception.GetType().FullName, exception.Message, _departedSilos.Keys))
            {
                // The request may still complete or may have been stored before the connection broke.
                // An in-flight append to a dead silo has the same uncertain outcome:
                // https://github.com/Cratis/Chronicle/issues/5231.
                RecordFailedAppend(id, exception);
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
            await outstanding.WaitAsync(TimeSpan.FromMinutes(2), _timeProvider);
        }
        catch (TimeoutException)
        {
            throw new InvalidOperationException("Appends were still outstanding two minutes after the load stopped.");
        }
    }

    void Failed(string reason, bool countsAgainstBudget = true)
    {
        Console.WriteLine($"Failed append: {reason}");

        // Departed-incarnation backoff is bounded by elapsed time, not the number of paced attempts.
        if (countsAgainstBudget && ++_failures > FailureBudgetPerTransition * transitions)
        {
            throw new InvalidOperationException($"More than {FailureBudgetPerTransition * transitions} append attempts failed during {transitions} membership transition(s).");
        }
    }

    [GeneratedRegex(@"Unable to connect to (?:endpoint )?S(?<address>[^:\s]+):\d+:(?<generation>-?\d+)[.,]", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex DepartedEndpoint { get; }

    sealed class FallbackActivitySource<T> : IActivitySource<T>
    {
        public ActivitySource ActualSource { get; } = new(typeof(T).FullName ?? typeof(T).Name);
    }
}
