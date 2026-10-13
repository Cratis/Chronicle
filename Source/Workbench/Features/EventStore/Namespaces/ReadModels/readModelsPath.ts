// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * Builds the router path of the Read Models page, optionally narrowed to a read model, an occurrence and an instance.
 *
 * The path is relative to the router, which owns the prefix the Workbench is served under, so it compares equal to
 * the location the router reports. It is not a URL to hand to the browser directly.
 * @param eventStore The event store.
 * @param namespace The namespace.
 * @param selection What is selected, from the read model downwards. Each part only applies when the one before it is set.
 * @returns The path.
 */
export function readModelsPath(
    eventStore: string | undefined,
    namespace: string | undefined,
    selection: { readModel?: string | null; occurrence?: string | null; instanceKey?: string | null } = {}
): string {
    let path = `/event-store/${eventStore}/${namespace}/read-models`;

    if (selection.readModel) {
        path += `/${selection.readModel}`;

        if (selection.occurrence) {
            path += `/${selection.occurrence}`;

            if (selection.instanceKey) {
                path += `/${selection.instanceKey}`;
            }
        }
    }

    return path;
}
