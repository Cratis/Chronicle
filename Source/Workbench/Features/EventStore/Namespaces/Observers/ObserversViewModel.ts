// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { inject, injectable } from 'tsyringe';
import { AllObservers } from 'Api/Observation';
import { ObserverInformation } from 'Api/Concepts/Observation/ObserverInformation';

@injectable()
export class ObserversViewModel {

    constructor(
        private readonly _allObservers: AllObservers,
        @inject('params') private readonly _params: any) {
        this._allObservers.subscribe(result => {
            this.observers = result.data;
        }, {
            eventStore: this._params.eventStoreId,
            namespace: this._params.namespace,
        });
    }

    observers: ObserverInformation[] = [];

    selectedObserver: ObserverInformation | undefined;
}
