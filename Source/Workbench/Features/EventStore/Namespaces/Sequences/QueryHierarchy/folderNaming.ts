// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { folderSegments, joinFolder } from './buildQueryTree';

/**
 * Work out the path a newly created folder should take, so that adding two folders in a row under
 * the same parent does not produce two nodes the user cannot tell apart.
 * @param parent The folder the new one goes under, or empty for the root of a scope.
 * @param name The name to start from.
 * @param existingPaths The folder paths that already exist within the scope.
 * @returns A path not already taken.
 */
export const uniqueFolderPath = (parent: string, name: string, existingPaths: string[]): string => {
    const taken = new Set(existingPaths);
    const candidate = joinFolder(parent, name);
    if (!taken.has(candidate)) return candidate;

    let suffix = 2;
    while (taken.has(joinFolder(parent, `${name} ${suffix}`))) suffix++;

    return joinFolder(parent, `${name} ${suffix}`);
};

/**
 * Work out the path a folder takes once its own segment is renamed, leaving its ancestors alone.
 * @param path The folder's current path.
 * @param name The new name for the folder's own segment.
 * @returns The new path.
 */
export const renamedFolderPath = (path: string, name: string): string => {
    const segments = folderSegments(path);
    segments[segments.length - 1] = name;

    return segments.join('/');
};

/**
 * Rewrite a folder path that sits at, or underneath, a folder that moved.
 * @param path The path to rewrite.
 * @param from The path of the folder that moved.
 * @param to Where it moved to.
 * @returns The rewritten path, or the original when it sits outside the folder that moved.
 */
export const rewriteFolderPath = (path: string, from: string, to: string): string => {
    if (path === from) return to;
    if (path.startsWith(`${from}/`)) return `${to}${path.slice(from.length)}`;

    return path;
};
