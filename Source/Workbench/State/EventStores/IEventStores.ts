// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { BehaviorSubject } from 'rxjs';

/**
 * Defines the system for managing event store global state.
 */
export abstract class IEventStores {

    /**
     * Gets the observable event stores.
     */
    abstract readonly eventStores: BehaviorSubject<string[]>;
}
