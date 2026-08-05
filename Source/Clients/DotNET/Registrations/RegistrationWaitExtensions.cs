// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Registrations;

/// <summary>
/// Helper extensions providing wait methods for artifact registration.
/// </summary>
/// <remarks>
/// These extensions are very useful for integration testing purposes, and for anything that has to treat "the read side
/// is up" as a fact before it measures anything.
/// </remarks>
public static class RegistrationWaitExtensions
{
    const int DefaultDelay = 50;

    /// <summary>
    /// Wait for artifact registration to have run for the event store, with an optional timeout.
    /// </summary>
    /// <param name="eventStore">Event store to wait for.</param>
    /// <param name="timeout">Optional timeout. If none is provided, it will default to 5 seconds.</param>
    /// <returns>The <see cref="RegistrationOutcome"/> observed once registration had run.</returns>
    /// <remarks>
    /// <para>
    /// Registration is a handler on the connection lifecycle, so from outside "has not run yet" and "never ran" look
    /// identical - this is what tells them apart. It waits for registration to have <em>run</em>, not to have
    /// succeeded: the returned outcome still has to be asked whether every artifact registered.
    /// </para>
    /// <para>
    /// A run that failed returns here too, carrying its <see cref="RegistrationOutcome.Failure"/> - the timeout is for
    /// a registration that never finished, not for one that finished badly. Ask <see cref="RegistrationOutcome"/> what
    /// happened rather than reading a returned value as success.
    /// </para>
    /// <para>
    /// Do not wait on <see cref="Connections.IConnectionLifecycle.IsConnected"/> instead. That flag is set to
    /// <see langword="true"/> before the connected handlers - registration among them - have run, and only rolled back
    /// once they have all finished, so a poll of it can return while registration is still in flight. See
    /// <see cref="RegistrationOutcome"/>.
    /// </para>
    /// </remarks>
    /// <exception cref="TaskCanceledException">Thrown when registration has not run within the timeout.</exception>
    public static async Task<RegistrationOutcome> WaitForRegistration(this IEventStore eventStore, TimeSpan? timeout = default)
    {
        timeout ??= TimeSpanFactory.DefaultTimeout();

        using var cts = new CancellationTokenSource(timeout.Value);
        while (true)
        {
            var outcome = eventStore.Registration;
            if (outcome.HasRun)
            {
                return outcome;
            }

            await Task.Delay(DefaultDelay, cts.Token);
        }
    }
}
