// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using SinkFailedPartition = Cratis.Chronicle.Storage.Sinks.FailedPartition;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    bool _deferAlertReports;

    /// <inheritdoc/>
    public async Task PartitionsFailed(IReadOnlyCollection<SinkFailedPartition> failedPartitions)
    {
        if (IsRetired || IsRemoving || _removed || failedPartitions.Count == 0)
        {
            return;
        }

        _deferAlertReports = true;
        try
        {
            foreach (var failure in failedPartitions)
            {
                await PartitionFailed(failure.EventSourceId, failure.EventSequenceNumber, [ProjectionBulkFailures.MessageFor(failure)], string.Empty, FailureKind.Handling);
            }
        }
        finally
        {
            _deferAlertReports = false;
            if (_alertReconciliationPending)
            {
                ScheduleAlertReport();
            }
        }
    }
}
