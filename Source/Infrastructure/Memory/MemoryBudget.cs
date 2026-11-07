// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using Cratis.DependencyInjection;

namespace Cratis.Chronicle.Memory;

/// <summary>
/// Represents an implementation of <see cref="IMemoryBudget"/>.
/// </summary>
[IgnoreConvention]
[SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "A reservation is handed to the caller, who disposes it to give the memory back.")]
public sealed class MemoryBudget : IMemoryBudget
{
    readonly object _lock = new();
    readonly LinkedList<Waiter> _waiters = new();
    readonly Func<long> _capacity;
    readonly TimeSpan? _maximumWait;
    readonly TimeProvider _timeProvider;
    readonly Histogram<double>? _waitTime;
    readonly Counter<long>? _queued;
    readonly Counter<long>? _overcommitted;
    long _reserved;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryBudget"/> class.
    /// </summary>
    /// <param name="capacity">Gives the number of bytes that can be reserved, asked every time room is looked for so it can follow memory pressure.</param>
    /// <param name="maximumWait">The longest a reservation waits before it is let through regardless, so a stuck holder can never stop all work. No limit when not set.</param>
    /// <param name="meter">Optional <see cref="Meter"/> to report the state of the budget to.</param>
    /// <param name="timeProvider">Optional <see cref="TimeProvider"/>.</param>
    public MemoryBudget(Func<long> capacity, TimeSpan? maximumWait = null, Meter? meter = null, TimeProvider? timeProvider = null)
    {
        _capacity = capacity;
        _maximumWait = maximumWait;
        _timeProvider = timeProvider ?? TimeProvider.System;

        if (meter is not null)
        {
            meter.CreateObservableGauge("cratis.chronicle.memory.budget.capacity", () => Capacity, "By", "Bytes that can be reserved right now");
            meter.CreateObservableGauge("cratis.chronicle.memory.budget.reserved", () => Reserved, "By", "Bytes currently reserved");
            meter.CreateObservableGauge("cratis.chronicle.memory.budget.waiting", () => Waiting, "{reservation}", "Reservations waiting for room");
            _waitTime = meter.CreateHistogram<double>("cratis.chronicle.memory.budget.wait_time", "ms", "Time a reservation waited for room");
            _queued = meter.CreateCounter<long>("cratis.chronicle.memory.budget.queued", "{reservation}", "Reservations that had to wait for room");
            _overcommitted = meter.CreateCounter<long>("cratis.chronicle.memory.budget.overcommitted", "{reservation}", "Reservations let through after the maximum wait");
        }
    }

    /// <inheritdoc/>
    public long Capacity => Math.Max(1, _capacity());

    /// <inheritdoc/>
    public long Reserved => Interlocked.Read(ref _reserved);

    /// <inheritdoc/>
    public int Waiting
    {
        get
        {
            lock (_lock)
            {
                return _waiters.Count;
            }
        }
    }

    /// <inheritdoc/>
    public bool TryReserve(long bytes, out MemoryReservation reservation)
    {
        if (bytes <= 0)
        {
            reservation = MemoryReservation.None;
            return true;
        }

        lock (_lock)
        {
            var wanted = Math.Min(bytes, Capacity);
            if (_waiters.Count == 0 && Fits(wanted))
            {
                _reserved += wanted;
                reservation = new MemoryReservation(this, wanted);
                return true;
            }
        }

        reservation = MemoryReservation.None;
        return false;
    }

    /// <inheritdoc/>
    public ValueTask<MemoryReservation> Reserve(long bytes, CancellationToken cancellationToken = default)
    {
        if (bytes <= 0)
        {
            return ValueTask.FromResult(MemoryReservation.None);
        }

        Waiter waiter;
        lock (_lock)
        {
            var wanted = Math.Min(bytes, Capacity);
            if (_waiters.Count == 0 && Fits(wanted))
            {
                _reserved += wanted;
                return ValueTask.FromResult(new MemoryReservation(this, wanted));
            }

            waiter = new Waiter(wanted, _timeProvider.GetTimestamp());
            waiter.Node = _waiters.AddLast(waiter);
        }

        _queued?.Add(1);
        return new ValueTask<MemoryReservation>(WaitFor(waiter, cancellationToken));
    }

    /// <summary>
    /// Take memory that is already in use into account.
    /// </summary>
    /// <param name="bytes">The number of bytes.</param>
    internal void Account(long bytes) => Interlocked.Add(ref _reserved, bytes);

    /// <summary>
    /// Give memory back and let the reservations that now fit through.
    /// </summary>
    /// <param name="bytes">The number of bytes.</param>
    internal void Release(long bytes)
    {
        Interlocked.Add(ref _reserved, -bytes);
        Pump();
    }

    bool Fits(long wanted)
    {
        var reserved = Interlocked.Read(ref _reserved);
        return reserved == 0 || reserved + wanted <= Capacity;
    }

    async Task<MemoryReservation> WaitFor(Waiter waiter, CancellationToken cancellationToken)
    {
        CancellationTokenSource? timeout = null;
        CancellationTokenRegistration timeoutRegistration = default;
        if (_maximumWait is { } maximumWait)
        {
            timeout = new CancellationTokenSource(maximumWait, _timeProvider);
            timeoutRegistration = timeout.Token.Register(() => Admit(waiter, overcommit: true));
        }

        await using var cancellation = cancellationToken.Register(() => Cancel(waiter, cancellationToken));
        try
        {
            var reservation = await waiter.Completion.Task.ConfigureAwait(false);
            _waitTime?.Record(_timeProvider.GetElapsedTime(waiter.Timestamp).TotalMilliseconds);
            return reservation;
        }
        finally
        {
            await timeoutRegistration.DisposeAsync().ConfigureAwait(false);
            timeout?.Dispose();
        }
    }

    void Cancel(Waiter waiter, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            if (waiter.Node?.List is not null)
            {
                _waiters.Remove(waiter.Node);
                waiter.Node = null;
            }
        }

        waiter.Completion.TrySetCanceled(cancellationToken);
        Pump();
    }

    void Admit(Waiter waiter, bool overcommit)
    {
        lock (_lock)
        {
            if (waiter.Node?.List is null)
            {
                return;
            }

            _waiters.Remove(waiter.Node);
            waiter.Node = null;
            _reserved += waiter.Bytes;
        }

        if (overcommit)
        {
            _overcommitted?.Add(1);
        }

        waiter.Completion.TrySetResult(new MemoryReservation(this, waiter.Bytes));
    }

    void Pump()
    {
        List<Waiter>? admitted = null;
        lock (_lock)
        {
            while (_waiters.First is { } first && Fits(first.Value.Bytes))
            {
                var waiter = first.Value;
                _waiters.RemoveFirst();
                waiter.Node = null;
                _reserved += waiter.Bytes;
                (admitted ??= []).Add(waiter);
            }
        }

        if (admitted is null)
        {
            return;
        }

        foreach (var waiter in admitted)
        {
            waiter.Completion.TrySetResult(new MemoryReservation(this, waiter.Bytes));
        }
    }

    sealed class Waiter(long bytes, long timestamp)
    {
        public long Bytes { get; } = bytes;

        public long Timestamp { get; } = timestamp;

        public LinkedListNode<Waiter>? Node { get; set; }

        public TaskCompletionSource<MemoryReservation> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
