// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Kernel.Integration.for_Observer.given;

/// <summary>
/// One of the reactors whose observers fall behind together in the catch-up cap specifications.
/// </summary>
/// <param name="gate">The <see cref="CatchUpGate"/> deliveries pass through.</param>
public class CatchUpCapReactorOne(CatchUpGate gate) : IReactor
{
    /// <summary>
    /// Handles the event.
    /// </summary>
    /// <param name="evt">The event.</param>
    /// <param name="context">The event context.</param>
    /// <returns>A task that completes when the event is handled.</returns>
    public Task OnQuarantineRecoveryEvent(QuarantineRecoveryEvent evt, EventContext context) => gate.Deliver(nameof(CatchUpCapReactorOne), evt.Name);
}
