// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { injectable } from 'tsyringe';
import { IEventStores } from 'State/EventStores/IEventStores';

@injectable()
export class HomeViewModel {
    constructor(eventStores: IEventStores) {
        eventStores.eventStores.subscribe(result => {
            this.eventStores = result;
        });
    }

    eventStores: string[] = [];
}
