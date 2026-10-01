// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    bool _retired;

    /// <inheritdoc/>
    public async Task Retire()
    {
        using var scope = logger.BeginObserverScope(_observerId, _observerKey);
        logger.RetiringObserver();

        await Unsubscribe();
        _retired = true;
        _isPreparingCatchup = false;

        // Normal unsubscription leaves Observing and its queue, but quarantine refuses Disconnected.
        // Do not clear quarantine or route: an observer quarantined mid-replay would resume its paused job.
        // The manager deletes those jobs after retirement. The replay flag stays unchanged.
        await DiscardFailedPartitions();
        await WriteStateAsync();
        await ReportAlertsRemoved();
    }
}
