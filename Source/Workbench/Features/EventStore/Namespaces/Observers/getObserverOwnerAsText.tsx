// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import strings from 'Strings';
import { ObserverOwner } from 'Api/Observation';

export const getObserverOwnerAsText = (owner: ObserverOwner) => {
    switch (owner) {
        case ObserverOwner.none:
            return strings.eventStore.namespaces.observers.owners.none;
        case ObserverOwner.client:
            return strings.eventStore.namespaces.observers.owners.client;
        case ObserverOwner.kernel:
            return strings.eventStore.namespaces.observers.owners.kernel;
    }
    return strings.eventStore.namespaces.observers.owners.none;
};
