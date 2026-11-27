// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ObserveEventStores } from '../../generated-input/EventStores/ObserveEventStores';

/**
 * Observes the event stores the Chronicle server offers, live, through the host's Arc query context.
 * @returns The event store names, in the order the query delivers them; empty until the first result arrives.
 */
export const useEventStoreNames = (): string[] => {
    const [result] = ObserveEventStores.use();
    return result.data.map(eventStore => eventStore.name);
};
