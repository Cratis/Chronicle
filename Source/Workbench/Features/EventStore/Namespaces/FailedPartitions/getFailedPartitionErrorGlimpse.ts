// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { FailedPartition } from 'Api/Observation';
import { getFailedPartitionAttemptsNewestFirst } from './getFailedPartitionAttemptsNewestFirst';

/**
 * Get a short glimpse of the error for a {@link FailedPartition} - the messages from the most recent attempt.
 * @param failedPartition The {@link FailedPartition} to get the glimpse for.
 * @returns The error messages of the most recent attempt joined by new lines, or an empty string when there are none.
 */
export const getFailedPartitionErrorGlimpse = (failedPartition: FailedPartition): string => {
    const [latestAttempt] = getFailedPartitionAttemptsNewestFirst(failedPartition);
    return latestAttempt?.messages.join('\n') ?? '';
};
