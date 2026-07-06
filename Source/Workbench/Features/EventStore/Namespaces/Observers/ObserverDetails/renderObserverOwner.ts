// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ObserverInformation, ObserverOwner } from 'Api/Observation';
import strings from 'Strings';

/**
 * Render the localized label for the observer owner.
 *
 * @param observer - The observer to render the owner of.
 * @returns The localized observer owner label, falling back to "none" when the owner is unrecognized.
 */
export const renderObserverOwner = (observer: ObserverInformation): string => {
    switch (observer.owner) {
        case ObserverOwner.none:
            return strings.eventStore.namespaces.observers.owners.none;
        case ObserverOwner.client:
            return strings.eventStore.namespaces.observers.owners.client;
        case ObserverOwner.kernel:
            return strings.eventStore.namespaces.observers.owners.kernel;
    }
    return strings.eventStore.namespaces.observers.owners.none;
};
