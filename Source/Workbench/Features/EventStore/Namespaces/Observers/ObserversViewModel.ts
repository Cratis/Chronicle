// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { inject, injectable } from 'tsyringe';
import { AllObservers } from 'Api/Observation';
import { ObserverInformation } from 'Api/Concepts/Observation/ObserverInformation';
import { INamespaces } from 'State/Namespaces';

@injectable()
export class ObserversViewModel {

    constructor(
        private readonly _allObservers: AllObservers,
        namespaces: INamespaces,
        @inject('params') private readonly _params: any) {


        namespaces.currentNamespace.subscribe(namespace => {
            this._allObservers.subscribe(result => {
                this.observers = result.data;
            }, {
                eventStore: this._params.eventStoreId,
                namespace: namespace.name
            });
        });
    }

    observers: ObserverInformation[] = [];

    selectedObserver: ObserverInformation | undefined;
}
