// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    /// <inheritdoc/>
    public async Task Retire()
    {
        ThrowIfSealed();
        using var scope = logger.BeginObserverScope(_observerId, _observerKey);
        logger.RetiringObserver();

        // Detach delivery before fallible job cleanup. Probing an absent namespace must not create state
        // or alert work, even when the shared projection definition exists elsewhere.
        await Unsubscribe();
        _isPreparingCatchup = false;
        if (!_observerExists) return;

        // Desired inactivity is forward-only: reconciliation or cleanup failure never restores Active.
        await CommitRetired();
        await RequireAlertReconciliation();
        await DiscardFailedPartitions(AlertClearedReason.Removed);
    }
}
