// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
import { ObserverRunningState } from '../../../../Api/Concepts/Observation/ObserverRunningState';


export const getObserverRunningStateAsText = (
    runningState: ObserverRunningState | string
) => {
    switch (runningState) {
        case ObserverRunningState.new:
            return 'New';
        case ObserverRunningState.routing:
            return 'Routing';
        case ObserverRunningState.replaying:
            return 'Replaying';
        case ObserverRunningState.catchingUp:
            return 'CatchingUp';
        case ObserverRunningState.active:
            return 'Active';
        case ObserverRunningState.paused:
            return 'Paused';
        case ObserverRunningState.stopped:
            return 'Stopped';
        case ObserverRunningState.suspended:
            return 'Suspended';
        case ObserverRunningState.failed:
            return 'Failed';
        case ObserverRunningState.tailOfReplay:
            return 'TailOfReplay';
        case ObserverRunningState.disconnected:
            return 'Disconnected';
        case ObserverRunningState.indexing:
            return 'Indexing';
    }
    return '[N/A]';
};
