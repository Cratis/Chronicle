// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import strings from 'Strings';
import { FailureKind } from 'Features/Concepts/Observation';

const kinds = strings.eventStore.namespaces.failedPartitionGroups.kinds;

export const getFailureKindLabel = (kind: FailureKind): string => {
    switch (kind) {
        case FailureKind.handling: return kinds.handling;
        case FailureKind.timeout: return kinds.timeout;
        case FailureKind.disconnected: return kinds.disconnected;
        case FailureKind.waitingForHandler: return kinds.waitingForHandler;
        default: return kinds.unknown;
    }
};
