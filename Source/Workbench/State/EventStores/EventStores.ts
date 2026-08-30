// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ObserveEventStores } from 'Features/EventStores/ObserveEventStores';
import { IEventStores } from './IEventStores';
import { BehaviorSubject } from 'rxjs';
import { injectable } from 'tsyringe';

/**
 * Represents an implementation of {@link IEventStores}
 */
@injectable()
export class EventStores implements IEventStores {
    private _eventStores: BehaviorSubject<string[]> = new BehaviorSubject<string[]>([]);

    constructor(allEventStores: ObserveEventStores) {
        allEventStores.subscribe(result => {
            this.eventStores.next(result.data.map(eventStore => eventStore.name));
        });
    }

    /** @inheritdoc */
    get eventStores(): BehaviorSubject<string[]> {
        return this._eventStores;
    }
}
