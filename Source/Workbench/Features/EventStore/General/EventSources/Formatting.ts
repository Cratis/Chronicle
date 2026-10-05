// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ConcurrencyDimensions, EventSourceOwner } from 'Features/Contracts/EventSources';
import strings from 'Strings';

const concurrencyDimensionLabels: [ConcurrencyDimensions, string][] = [
    [ConcurrencyDimensions.eventSourceId, strings.eventStore.general.eventSources.concurrencyDimensions.eventSourceId],
    [ConcurrencyDimensions.eventSourceType, strings.eventStore.general.eventSources.concurrencyDimensions.eventSourceType],
    [ConcurrencyDimensions.eventStreamType, strings.eventStore.general.eventSources.concurrencyDimensions.eventStreamType],
    [ConcurrencyDimensions.eventStreamId, strings.eventStore.general.eventSources.concurrencyDimensions.eventStreamId]
];

const eventSourceOwnerLabels: Record<EventSourceOwner, string> = {
    [EventSourceOwner.none]: strings.eventStore.general.eventSources.owners.none,
    [EventSourceOwner.client]: strings.eventStore.general.eventSources.owners.client,
    [EventSourceOwner.kernel]: strings.eventStore.general.eventSources.owners.kernel
};

export const formatConcurrency = (concurrency: ConcurrencyDimensions) => {
    const dimensions = concurrencyDimensionLabels
        .filter(([dimension]) => (concurrency & dimension) === dimension)
        .map(([, label]) => label);

    return dimensions.length > 0
        ? dimensions.join(', ')
        : strings.eventStore.general.eventSources.concurrencyDimensions.none;
};

export const formatOwner = (owner: EventSourceOwner) => eventSourceOwnerLabels[owner];
