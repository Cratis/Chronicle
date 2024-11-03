// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Namespace } from 'Api/Namespaces';
import { BehaviorSubject } from 'rxjs';

/**
 * Defines the system for managing namespace global state.
 */
export abstract class INamespaces {
    /**
     * Gets the current namespace value.
     */
    abstract readonly currentNamespace: BehaviorSubject<Namespace>;

    /**
     * Set the current namespace.
     */
    abstract readonly setCurrentNamespace: (namespace: Namespace) => void;

    abstract readonly namespaces: BehaviorSubject<Namespace[]>;
}

