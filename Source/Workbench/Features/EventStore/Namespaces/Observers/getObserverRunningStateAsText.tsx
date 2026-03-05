// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import strings from 'Strings';
import { ObserverRunningState } from 'Api/Observation/ObserverRunningState';

export const getObserverRunningStateAsText = (
    runningState: ObserverRunningState | string
) => {
    switch (runningState) {
        case ObserverRunningState.unknown:
            return strings.eventStore.namespaces.observers.states.unknown;
        case ObserverRunningState.active:
            return strings.eventStore.namespaces.observers.states.active;
        case ObserverRunningState.suspended:
            return strings.eventStore.namespaces.observers.states.suspended;
        case ObserverRunningState.replaying:
            return strings.eventStore.namespaces.observers.states.replaying;
        case ObserverRunningState.disconnected:
            return strings.eventStore.namespaces.observers.states.disconnected;
    }
    return '[N/A]';
};
