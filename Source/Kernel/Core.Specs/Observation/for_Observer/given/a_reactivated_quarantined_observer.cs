// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.given;

/// <summary>
/// A quarantined observer whose grain was deactivated and activated again, with nobody having subscribed or cleared the quarantine.
/// </summary>
public class a_reactivated_quarantined_observer : an_observer_with_subscription
{
    async Task Establish()
    {
        await _observer.TransitionTo<QuarantinedObserver>();
        await Reactivate();
    }
}
