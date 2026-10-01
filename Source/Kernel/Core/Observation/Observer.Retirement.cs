// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    /// <inheritdoc/>
    public async Task Retire()
    {
        ThrowIfRemoving();
        using var scope = logger.BeginObserverScope(_observerId, _observerKey);
        logger.RetiringObserver();

        await _alertMutationLock.WaitAsync();
        try
        {
            _alertDisposition = AlertDisposition.Retired;
            ChangeAlertState();
            await WriteStateAsync();
        }
        finally
        {
            _alertMutationLock.Release();
        }

        await Unsubscribe();
        _isPreparingCatchup = false;

        // Operational quarantine and replay stay retained, but the durable disposition no longer desires alerts.
        await RequireAlertReconciliation();
        await DiscardFailedPartitions(AlertClearedReason.Removed);
    }
}
