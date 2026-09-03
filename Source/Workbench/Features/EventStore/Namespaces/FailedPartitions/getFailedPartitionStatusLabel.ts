// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import strings from 'Strings';
import { FailedPartitionStatus } from './FailedPartitionStatus';

export const getFailedPartitionStatusLabel = (status: FailedPartitionStatus): string => {
    // SAFETY: the locale defines a label for every string-valued FailedPartitionStatus member.
    const failedPartitionStrings = strings.eventStore.namespaces
        .failedPartitions as unknown as {
        statuses: Record<FailedPartitionStatus, string>;
    };

    return failedPartitionStrings.statuses[status];
};
