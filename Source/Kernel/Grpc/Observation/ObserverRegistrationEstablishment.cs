// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Observation;

/// <summary>
/// Establishes a client observer's subscription for an observation stream, riding out a busy observer.
/// </summary>
/// <remarks>
/// <para>
/// Establishing a reactor or reducer subscription is a grain call that waits for the observer grain, which on a cold or
/// saturated silo can be busy for longer than the grain call's response timeout. Failing the stream on that timeout used
/// to feed a loop that never converged: Orleans does not cancel a call that timed out, so it still ran; the failed stream
/// released its registration with an unsubscribe queued behind it; and the client re-registered on the same connection a
/// moment later, queuing another subscribe behind both. Every retry added work to the grains it was waiting for.
/// </para>
/// <para>
/// So a timeout keeps the stream and asks again. Establishing is idempotent - an unchanged definition on an already
/// subscribed observer returns at once - and the new call queues behind the one that timed out, so it answers as soon as
/// that one has run, without adding an unsubscribe and a re-registration to the queue. The number of attempts is bounded,
/// so an observer that stays wedged still fails the stream and leaves the next attempt to the client's backoff.
/// </para>
/// <para>
/// Between attempts the caller gets a turn to give back what it holds while it waits - the registration gate slot - so
/// a wedged observer does not keep healthy registrations out for every attempt it makes.
/// </para>
/// </remarks>
internal static class ObserverRegistrationEstablishment
{
    /// <summary>
    /// The most attempts made on one stream before its failure is surfaced to the client.
    /// </summary>
    internal const int MaxAttempts = 4;

    /// <summary>
    /// Establishes the subscription, asking again on the same stream when an attempt times out.
    /// </summary>
    /// <param name="establish">The idempotent call establishing the subscription.</param>
    /// <param name="attemptTimedOut">Called with the attempt number and its timeout when an attempt is tried again.</param>
    /// <param name="betweenAttempts">Called before an attempt that follows a timed-out one, such as to re-enter the registration gate.</param>
    /// <param name="cancellationToken">The stream's cancellation; once cancelled, a timeout is not tried again.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="TimeoutException">The last attempt timed out.</exception>
    internal static async Task Establish(
        Func<Task> establish,
        Action<int, TimeoutException> attemptTimedOut,
        Func<Task> betweenAttempts,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await establish();
                return;
            }
            catch (TimeoutException exception) when (attempt < MaxAttempts && !cancellationToken.IsCancellationRequested)
            {
                attemptTimedOut(attempt, exception);
                await betweenAttempts();
            }
        }
    }
}
