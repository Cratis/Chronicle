// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { BehaviorSubject } from 'rxjs';

/**
 * Defines the system for managing namespace global state.
 */
export abstract class INamespaces {
    /**
     * Gets the current namespace value.
     */
    abstract readonly currentNamespace: BehaviorSubject<string>;

    /**
     * Set the current namespace.
     */
    abstract readonly setCurrentNamespace: (namespace: string) => void;

    /**
     * Gets the observable namespaces.
     */
    abstract readonly namespaces: BehaviorSubject<string[]>;
}

