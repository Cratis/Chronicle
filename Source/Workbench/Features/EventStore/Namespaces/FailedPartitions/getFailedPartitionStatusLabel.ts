// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import strings from 'Strings';
import { FailedPartitionStatus } from './FailedPartitionStatus';

export const getFailedPartitionStatusLabel = (status: FailedPartitionStatus): string =>
    strings.eventStore.namespaces.failedPartitions.statuses[status];
