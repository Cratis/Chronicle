// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { injectable } from 'tsyringe';
import { AllObservers } from 'API/events/store/observers/AllObservers';
import { ObserverInformation } from 'API/events/store/observers/ObserverInformation';
import { QueryResult } from '@aksio/applications/queries';

@injectable()
export class ObserversViewModel {

    constructor(private readonly _allObservers: AllObservers) {
        this._allObservers.subscribe(_ => {
            const result = _ as unknown as QueryResult<ObserverInformation[]>;
            this.observers = result.data;
        }, {
            microserviceId: '00000000-0000-0000-0000-000000000000',
            tenantId: '00000000-0000-0000-0000-000000000000',
        });
    }

    observers: ObserverInformation[] = [];
}
