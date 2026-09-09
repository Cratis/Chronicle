// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { FailedPartitionSeverity } from './FailedPartitionSeverity';
import { FailedPartitionStatus } from './FailedPartitionStatus';

export const getFailedPartitionSeverity = (
    status: FailedPartitionStatus,
): FailedPartitionSeverity => {
    switch (status) {
        case FailedPartitionStatus.Resolved:
            return FailedPartitionSeverity.Success;
        case FailedPartitionStatus.Quarantined:
            return FailedPartitionSeverity.Danger;
        case FailedPartitionStatus.Failed:
            return FailedPartitionSeverity.Warning;
        case FailedPartitionStatus.Unknown:
            return FailedPartitionSeverity.Secondary;
    }
};
