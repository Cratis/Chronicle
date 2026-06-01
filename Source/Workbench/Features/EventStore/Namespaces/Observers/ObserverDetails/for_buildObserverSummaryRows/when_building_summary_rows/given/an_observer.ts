// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ObserverInformation, ObserverOwner, ObserverRunningState, ObserverType } from 'Api/Observation';

export class an_observer {
    observer: ObserverInformation;

    constructor() {
        this.observer = new ObserverInformation();
        this.observer.id = 'observer-id';
        this.observer.type = ObserverType.projection;
        this.observer.owner = ObserverOwner.kernel;
        this.observer.runningState = ObserverRunningState.active;
        this.observer.eventSequenceId = 'event-log';
        this.observer.nextEventSequenceNumber = 42;
        this.observer.lastHandledEventSequenceNumber = 41;
        this.observer.tailEventSequenceNumber = 100;
        this.observer.isSubscribed = true;
        this.observer.isReplayable = false;
    }
}
