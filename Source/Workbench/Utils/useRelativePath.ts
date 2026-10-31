// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * Builds a root-relative path for a route inside the Workbench.
 *
 * The router's basename carries the prefix the Workbench is served under, so this deliberately adds nothing of
 * its own - adding it here as well would put the prefix in the address twice. Use `absolutePath` for anything
 * that leaves the router, such as a fetch or a full page load.
 * @param path The route, with or without a leading slash.
 * @returns The path to hand to the router.
 */
export const useRelativePath = (path: string) => `/${path}`.replace(/\/+/g, '/');
