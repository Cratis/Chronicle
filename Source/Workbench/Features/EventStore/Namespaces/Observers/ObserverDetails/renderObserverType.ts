// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ObserverInformation, ObserverType } from 'Api/Observation';
import strings from 'Strings';

/**
 * Render the localized label for the observer type.
 *
 * @param observer - The observer to render the type of.
 * @returns The localized observer type label, falling back to "unknown" when the type is unrecognized.
 */
export const renderObserverType = (observer: ObserverInformation): string => {
    switch (observer.type) {
        case ObserverType.reactor:
            return strings.eventStore.namespaces.observers.types.reactor;
        case ObserverType.projection:
            return strings.eventStore.namespaces.observers.types.projection;
        case ObserverType.reducer:
            return strings.eventStore.namespaces.observers.types.reducer;
        case ObserverType.external:
            return strings.eventStore.namespaces.observers.types.external;
    }
    return strings.eventStore.namespaces.observers.types.unknown;
};
