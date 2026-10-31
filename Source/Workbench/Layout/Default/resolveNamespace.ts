// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * Decides which namespace the Workbench is working in.
 *
 * The address is the source of truth: a namespace in the route always wins, whether or not the list of
 * namespaces has arrived yet, so a refreshed or shared link opens exactly where it points. Only when the
 * route names no namespace (the event store level pages) does the last chosen one carry over, and only
 * when it still exists.
 * @param routeNamespace The namespace in the route, if there is one.
 * @param remembered The namespace chosen last time in this event store, if any.
 * @param available The namespaces that exist in the event store, empty while they are still loading.
 * @returns The namespace to work in, or an empty string when there is nothing to choose from.
 */
export const resolveNamespace = (
    routeNamespace: string | undefined,
    remembered: string | undefined,
    available: readonly string[]): string => {

    if (routeNamespace) {
        return routeNamespace;
    }

    if (available.length === 0) {
        return remembered ?? '';
    }

    const match = (name: string | undefined) =>
        name ? available.find(candidate => candidate.toLowerCase() === name.toLowerCase()) : undefined;

    return match(remembered) ?? match('Default') ?? available[0];
};
