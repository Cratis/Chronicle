// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Testing.Reactors;

/// <summary>
/// A test reactor that records the identity of every delivery it is handed, so a spec can see what the scenario
/// presents as one delivery and what it presents as the next.
/// </summary>
/// <param name="log">The <see cref="DeliveryLog"/> the identities are recorded in.</param>
public class DeliveryRecordingReactor(DeliveryLog log) : IReactor
{
    /// <summary>
    /// Records the delivery of a <see cref="VibeStarted"/> event.
    /// </summary>
    /// <param name="event">The triggering <see cref="VibeStarted"/> event.</param>
    /// <param name="delivery">The <see cref="ReactorDelivery"/> identifying this delivery.</param>
    public void VibeStarted(VibeStarted @event, ReactorDelivery delivery) => log.Record(delivery.Id);
}
