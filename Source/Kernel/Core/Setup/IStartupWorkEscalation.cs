// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Orleans.Hosting;

/// <summary>
/// Defines what happens when recoverable startup work keeps failing and when it recovers.
/// </summary>
/// <remarks>
/// This is the single seam between the background startup pass and whatever tells an operator about it. Today
/// the implementation writes structured logs; raising a real alert is a follow-up that plugs in here without
/// touching the retry loop. Implementations must be cheap and must not throw.
/// </remarks>
internal interface IStartupWorkEscalation
{
    /// <summary>
    /// Called once an item has failed enough consecutive times to need attention, then again at most once per backoff cap interval while it keeps failing.
    /// </summary>
    /// <param name="item">The <see cref="StartupWorkItem"/> that keeps failing.</param>
    /// <param name="attempts">The number of consecutive failed attempts so far.</param>
    /// <param name="nextDelay">How long the next retry waits.</param>
    /// <param name="lastException">The most recent failure.</param>
    void Escalate(StartupWorkItem item, int attempts, TimeSpan nextDelay, Exception lastException);

    /// <summary>
    /// Called when an item that was escalated finally succeeds.
    /// </summary>
    /// <param name="item">The <see cref="StartupWorkItem"/> that recovered.</param>
    /// <param name="attempts">The number of attempts it took, including the successful one.</param>
    void Recovered(StartupWorkItem item, int attempts);
}
