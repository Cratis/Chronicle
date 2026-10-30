// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Coordinates a transient subscription gap independently of zero-work backoff.
/// </summary>
internal interface ILiveSubscriptionRecovery
{
    /// <summary>
    /// Gets whether a subscription gap still needs authoritative recovery.
    /// </summary>
    bool HasSubscriptionGap { get; }

    /// <summary>
    /// Captures the earliest position potentially missed by a lost subscription.
    /// </summary>
    /// <param name="position">The first unresolved position.</param>
    void SubscriptionLost(EventSequenceNumber position);

    /// <summary>
    /// Checks the gap after subscribing and starts an eligible recovery attempt.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task RecoverSubscriptionGap();
}
