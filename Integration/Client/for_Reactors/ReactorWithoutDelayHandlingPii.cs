// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Integration.for_Reactors;

/// <summary>
/// Reactor that handles PII events without delay, used in integration tests.
/// </summary>
[DependencyInjection.IgnoreConvention]
public class ReactorWithoutDelayHandlingPii : IReactor
{
    /// <summary>
    /// Gets the number of handled events.
    /// </summary>
    public int HandledEvents;

    /// <summary>
    /// Gets the last social security number received.
    /// </summary>
    public string LastSocialSecurityNumber = string.Empty;

    /// <summary>
    /// Handles a <see cref="PiiEvent"/>.
    /// </summary>
    /// <param name="evt">The event.</param>
    /// <param name="ctx">The event context.</param>
    /// <returns>A completed task.</returns>
    public Task OnPiiEvent(PiiEvent evt, EventContext ctx)
    {
        Interlocked.Increment(ref HandledEvents);
        LastSocialSecurityNumber = evt.SocialSecurityNumber;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Waits until the handled event count reaches the specified value.
    /// </summary>
    /// <param name="count">Target event count.</param>
    /// <param name="timeout">Optional timeout.</param>
    /// <returns>A <see cref="Task"/> that completes when the count is reached.</returns>
    public async Task WaitTillHandledEventReaches(int count, TimeSpan? timeout = default)
    {
        timeout ??= TimeSpanFactory.DefaultTimeout();
        using var cts = new CancellationTokenSource(timeout.Value);
        while (HandledEvents < count)
        {
            await Task.Delay(50, cts.Token);
        }
    }
}
