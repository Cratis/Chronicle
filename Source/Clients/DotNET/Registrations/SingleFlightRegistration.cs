// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Registrations;

/// <summary>
/// Runs a registration action as a single flight: callers that arrive while a run is in flight await that same run
/// instead of starting another, and a run that follows a failed one waits a backoff delay before hitting the wire.
/// </summary>
/// <remarks>
/// Registration requests are heavy on the kernel side and idempotent from the caller's point of view, so there is
/// never a reason to have two of them from the same client in flight at once. Stacking them is actively harmful: a
/// request that timed out client-side is usually still queued kernel-side, and every immediate retry adds the same
/// workload to a queue that is already too slow to answer - the feedback loop behind a registration storm. Sharing
/// the in-flight run and pacing the next one with <see cref="RegistrationBackoff"/> breaks that loop at the client.
/// </remarks>
/// <param name="action">The registration action to run.</param>
/// <param name="backoff">Optional <see cref="RegistrationBackoff"/> deciding the delay after failed runs.</param>
/// <param name="delay">Optional delay implementation, for deciding how to wait. Defaults to <see cref="Task.Delay(TimeSpan)"/>.</param>
internal sealed class SingleFlightRegistration(Func<Task> action, RegistrationBackoff? backoff = null, Func<TimeSpan, Task>? delay = null)
{
    readonly RegistrationBackoff _backoff = backoff ?? new();
    readonly Func<TimeSpan, Task> _delay = delay ?? (toWait => Task.Delay(toWait));
    readonly object _gate = new();
    Task? _inFlight;
    int _consecutiveFailures;

    /// <summary>
    /// Run the registration action, or join the run that is already in flight.
    /// </summary>
    /// <returns>The task representing the shared run; it faults when the action does.</returns>
    public Task Run()
    {
        lock (_gate)
        {
            _inFlight ??= RunSingleFlight();
            return _inFlight;
        }
    }

    async Task RunSingleFlight()
    {
        // Leave the caller's lock before doing any work, so the action never runs while the gate is held.
        await Task.Yield();

        try
        {
            var toWait = _backoff.NextDelay(_consecutiveFailures);
            if (toWait > TimeSpan.Zero)
            {
                await _delay(toWait);
            }

            await action();
            _consecutiveFailures = 0;
        }
        catch
        {
            _consecutiveFailures++;
            throw;
        }
        finally
        {
            lock (_gate)
            {
                _inFlight = null;
            }
        }
    }
}
