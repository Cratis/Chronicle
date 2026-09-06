// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { FailedPartitionDetails as FailedPartition, FailedPartitionAttemptDetails as FailedPartitionAttempt } from 'Features/Observation';

/**
 * Get the attempts of a {@link FailedPartition} ordered with the most recent attempt first.
 * @param failedPartition The {@link FailedPartition} to get the attempts for.
 * @returns The attempts ordered newest first.
 */
export const getFailedPartitionAttemptsNewestFirst = (failedPartition: FailedPartition): FailedPartitionAttempt[] =>
    [...failedPartition.attempts].reverse();
