// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

const keyFor = (eventStore: string) => `chronicle.workbench.namespace.${eventStore}`;

/**
 * Reads the namespace last chosen in an event store.
 * @param eventStore The event store the namespace belongs to.
 * @returns The namespace, or undefined when none has been chosen or storage is unavailable.
 */
export const readRememberedNamespace = (eventStore: string): string | undefined => {
    try {
        return localStorage.getItem(keyFor(eventStore)) ?? undefined;
    } catch {
        return undefined;
    }
};

/**
 * Remembers the namespace chosen in an event store, so event store level pages come back to it.
 * @param eventStore The event store the namespace belongs to.
 * @param namespace The namespace to remember.
 */
export const writeRememberedNamespace = (eventStore: string, namespace: string) => {
    try {
        localStorage.setItem(keyFor(eventStore), namespace);
    } catch {
        // Remembering is a convenience; a browser that refuses storage still works from the address.
    }
};
