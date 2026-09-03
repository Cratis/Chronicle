// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { FailedPartition } from 'Api/Observation';
import { FailedPartitionStatus } from './FailedPartitionStatus';

export const getFailedPartitionStatus = (
    failedPartition: FailedPartition,
): FailedPartitionStatus => {
    if (failedPartition.isResolved) return FailedPartitionStatus.Resolved;
    if (failedPartition.isQuarantined === true) return FailedPartitionStatus.Quarantined;
    if (failedPartition.isQuarantined === false) return FailedPartitionStatus.Failed;

    return FailedPartitionStatus.Unknown;
};
