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

    /**
     * Notifies the service that the active event store has changed, triggering subscription if needed.
     * @param eventStore - The name of the now-active event store.
     */
    abstract setEventStore(eventStore: string): void;

    /**
     * Tells the service which namespace the route currently names, so it can settle on a namespace
     * without holding a stale one of its own.
     * @param namespace - The namespace in the current route, or undefined on a route that has none.
     */
    abstract setRouteNamespace(namespace: string | undefined): void;
}

