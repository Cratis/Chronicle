// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { injectable } from 'tsyringe';
import { AllObservers } from 'API/Observation';
import { ObserverInformation } from 'API/Cratis/Kernel/Contracts/Observation/ObserverInformation';

@injectable()
export class ObserversViewModel {

    constructor(private readonly _allObservers: AllObservers) {
        this._allObservers.subscribe(result => {
            this.observers = result.data;
        }, {
            eventStore: '00000000-0000-0000-0000-000000000000',
            namespace: '00000000-0000-0000-0000-000000000000',
        });
    }

    observers: ObserverInformation[] = [];

    selectedObserver: ObserverInformation | undefined;
}
