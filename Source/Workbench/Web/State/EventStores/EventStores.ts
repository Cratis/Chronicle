// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AllEventStores } from 'Api/EventStores/AllEventStores';
import { IEventStores } from './IEventStores';
import { BehaviorSubject } from 'rxjs';
import { injectable } from 'tsyringe';

/**
 * Represents an implementation of {@link IEventStores}
 */
@injectable()
export class EventStores implements IEventStores {
    private _eventStores: BehaviorSubject<string[]> = new BehaviorSubject<string[]>([]);

    constructor(allEventStores: AllEventStores) {
        allEventStores.subscribe(result => {
            this.eventStores.next(result.data);
        });
    }

    /** @inheritdoc */
    get eventStores(): BehaviorSubject<string[]> {
        return this._eventStores;
    }
}
