// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Registrations;

/// <summary>
/// Bounds how many distinct registrations are applied at the same time, queues the rest in arrival order and makes
/// every identical registration join the one that is already queued or running.
/// </summary>
/// <remarks>
/// A registration is identified by its key, usually its signature. The key is looked up before the first await, so a
/// retry, or the same registration from another namespace, joins the flight in progress instead of starting its own.
/// A caller that stops waiting leaves its flight; the flight is abandoned when its last caller has left: dropped from
/// the queue if it has not started, cancelled through its token if it has. A flight that waited longer than the queue
/// age fails with <see cref="RegistrationBusyException"/>. The queue is meant to be used from one activation, but
/// is safe to call from several threads.
/// </remarks>
/// <param name="limit">How many flights may run at once.</param>
/// <param name="maxQueueAge">How long a flight may wait for a place before it fails as busy.</param>
/// <param name="retryAfter">What a busy failure tells the caller to wait before retrying.</param>
/// <param name="timeProvider">The <see cref="TimeProvider"/> used to age the queue.</param>
public sealed class RegistrationQueue(int limit, TimeSpan maxQueueAge, TimeSpan retryAfter, TimeProvider timeProvider)
{
    readonly Lock _lock = new();
    readonly Dictionary<string, Flight> _flights = [];
    readonly LinkedList<Flight> _waiting = [];
    int _running;

    /// <summary>
    /// Gets how many flights are running now.
    /// </summary>
    public int Running
    {
        get
        {
            lock (_lock)
            {
                return _running;
            }
        }
    }

    /// <summary>
    /// Gets how many flights are waiting for a place.
    /// </summary>
    public int Waiting
    {
        get
        {
            lock (_lock)
            {
                return _waiting.Count;
            }
        }
    }

    /// <summary>
    /// Joins the flight for a key when there is one.
    /// </summary>
    /// <param name="key">The key of the registration.</param>
    /// <param name="caller">The caller's <see cref="CancellationToken"/>.</param>
    /// <param name="joined">A task that completes with the flight, when there is one.</param>
    /// <returns>True when a flight is queued or running for the key and has been joined; false otherwise.</returns>
    public bool TryJoin(string key, CancellationToken caller, out Task joined)
    {
        Flight? flight;
        lock (_lock)
        {
            if (!_flights.TryGetValue(key, out flight))
            {
                joined = Task.CompletedTask;
                return false;
            }

            flight.Callers++;
        }

        joined = WaitFor(flight, caller);
        return true;
    }

    /// <summary>
    /// Runs the work for a key, or joins it when the same key is queued or running.
    /// </summary>
    /// <param name="key">The key of the registration.</param>
    /// <param name="work">The work, given a token that is cancelled when every caller has left.</param>
    /// <param name="caller">The caller's <see cref="CancellationToken"/>.</param>
    /// <returns>A task that completes when the work has.</returns>
    /// <exception cref="RegistrationBusyException">The registration waited for a place for longer than the queue age.</exception>
    public Task Run(string key, Func<CancellationToken, Task> work, CancellationToken caller = default)
    {
        Flight flight;
        lock (_lock)
        {
            if (_flights.TryGetValue(key, out var existing))
            {
                existing.Callers++;
                flight = existing;
            }
            else
            {
                flight = new Flight(key, work);
                _flights[key] = flight;
                _waiting.AddLast(flight.Node);
                flight.AgeTimer = timeProvider.CreateTimer(_ => Expire(flight), null, maxQueueAge, Timeout.InfiniteTimeSpan);
                Pump();
            }
        }

        return WaitFor(flight, caller);
    }

    async Task WaitFor(Flight flight, CancellationToken caller)
    {
        try
        {
            await flight.Completion.Task.WaitAsync(caller);
        }
        catch (OperationCanceledException) when (caller.IsCancellationRequested)
        {
            Leave(flight);
            throw;
        }
    }

    void Leave(Flight flight)
    {
        lock (_lock)
        {
            flight.Callers--;
            if (flight.Callers > 0 || flight.IsFinished)
            {
                return;
            }

            if (flight.Node.List is not null)
            {
                _waiting.Remove(flight.Node);
                Finish(flight);
                flight.Completion.TrySetCanceled();
                return;
            }
        }

        flight.Cancellation.Cancel();
    }

    void Expire(Flight flight)
    {
        lock (_lock)
        {
            if (flight.Node.List is null || flight.IsFinished)
            {
                return;
            }

            _waiting.Remove(flight.Node);
            Finish(flight);
        }

        flight.Completion.TrySetException(new RegistrationBusyException(retryAfter));
    }

    /// <summary>
    /// Starts waiting flights while there are places for them. Called with the lock held.
    /// </summary>
    void Pump()
    {
        while (_running < limit && _waiting.First is { } next)
        {
            _waiting.RemoveFirst();
            _running++;
            next.Value.AgeTimer?.Dispose();
            _ = Execute(next.Value);
        }
    }

    async Task Execute(Flight flight)
    {
        Exception? failure = null;
        try
        {
            await flight.Work(flight.Cancellation.Token);
        }
        catch (Exception exception)
        {
            failure = exception;
        }

        lock (_lock)
        {
            _running--;
            Finish(flight);
            Pump();
        }

        if (failure is null)
        {
            flight.Completion.TrySetResult();
        }
        else if (failure is OperationCanceledException)
        {
            flight.Completion.TrySetCanceled();
        }
        else
        {
            flight.Completion.TrySetException(failure);
        }
    }

    /// <summary>
    /// Takes a flight out of the table. Called with the lock held. The flight leaves the table before its callers are
    /// answered, so a registration that arrives afterwards leads its own.
    /// </summary>
    /// <param name="flight">The flight that is over.</param>
    void Finish(Flight flight)
    {
        flight.IsFinished = true;
        flight.AgeTimer?.Dispose();
        _flights.Remove(flight.Key);
    }

    sealed class Flight
    {
        public Flight(string key, Func<CancellationToken, Task> work)
        {
            Key = key;
            Work = work;
            Node = new LinkedListNode<Flight>(this);
        }

        public string Key { get; }
        public Func<CancellationToken, Task> Work { get; }
        public LinkedListNode<Flight> Node { get; }
        public TaskCompletionSource Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public CancellationTokenSource Cancellation { get; } = new();
        public ITimer? AgeTimer { get; set; }
        public int Callers { get; set; } = 1;
        public bool IsFinished { get; set; }
    }
}
