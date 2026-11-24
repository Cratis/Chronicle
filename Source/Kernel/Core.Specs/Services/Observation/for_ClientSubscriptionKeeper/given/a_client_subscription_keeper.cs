// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Supervision;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Services.Observation.for_ClientSubscriptionKeeper.given;

/// <summary>
/// Keeps a subscription on a supervisor that only records what the keeper registers: the specification runs the checks
/// itself and reads from each what the keeper asks to wait before the next.
/// </summary>
public class a_client_subscription_keeper : Specification
{
    protected const double Draw = 0.5;
    protected static readonly TimeSpan Revise = TimeSpan.FromSeconds(2);
    protected static readonly TimeSpan SafetyNet = TimeSpan.FromSeconds(60);

    /// <summary>
    /// The safety-net wait with the draw the schedule is given: between three quarters of the interval and all of it.
    /// </summary>
    protected static readonly TimeSpan SafetyNetWait = SafetyNet * (SubscriptionCheckSchedule.SafetyNetFloor + ((1 - SubscriptionCheckSchedule.SafetyNetFloor) * Draw));

    protected int _checks;
    protected ISiloSupervisor _supervisor;
    protected string _key;
    protected TimeSpan? _registeredInterval;
    protected Func<Task<TimeSpan?>> _periodicCheck;
    protected Func<Task> _checkAfterDeparture;
    protected readonly CancellationTokenSource _streamClosed = new();
    protected Task _keeping = Task.CompletedTask;
    protected int _unregistered;

    /// <summary>
    /// Gets the longest a check may run; specifications that hang a check override it to keep it short.
    /// </summary>
    protected virtual TimeSpan? CheckTimeout => null;

    void Establish()
    {
        _supervisor = Substitute.For<ISiloSupervisor>();
        _supervisor
            .When(supervisor => supervisor.Register(Arg.Any<string>(), Arg.Any<TimeSpan?>(), Arg.Any<Func<Task<TimeSpan?>>>(), Arg.Any<Func<Task>>()))
            .Do(call =>
            {
                _key = call.ArgAt<string>(0);
                _registeredInterval = call.ArgAt<TimeSpan?>(1);
                _periodicCheck = call.ArgAt<Func<Task<TimeSpan?>>>(2);
                _checkAfterDeparture = call.ArgAt<Func<Task>>(3);
            });
        _supervisor.When(supervisor => supervisor.Unregister(Arg.Any<string>())).Do(_ => Interlocked.Increment(ref _unregistered));
    }

    async Task Destroy()
    {
        await _streamClosed.CancelAsync();
        await _keeping.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _streamClosed.Dispose();
    }

    /// <summary>
    /// Performs one check.
    /// </summary>
    /// <param name="check">The number of the check, starting at 1.</param>
    /// <returns>Whether the subscription was found intact.</returns>
    protected virtual Task<bool> Check(int check) => Task.FromResult(false);

    /// <summary>
    /// Starts keeping the subscription and waits until the keeper has registered with the supervisor.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    protected async Task StartKeeping()
    {
        _keeping = ClientSubscriptionKeeper.KeepWhileConnected(
            () => Check(Interlocked.Increment(ref _checks)),
            new SubscriptionCheckSchedule(Revise, SafetyNet, () => Draw),
            _supervisor,
            "the-client",
            NullLogger.Instance,
            _streamClosed.Token,
            CheckTimeout);
        var deadline = DateTimeOffset.UtcNow.AddSeconds(5);
        while (_periodicCheck is null && DateTimeOffset.UtcNow < deadline)
        {
            await Task.Delay(5);
        }
    }
}
