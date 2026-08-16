// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * Resolves a possibly dotted field path (`observer.runningState`) against a row.
 *
 * PrimeReact 10 resolved nested column fields for us. PrimeReact 11's headless
 * table leaves cell content to the caller, so the Workbench tables resolve the
 * path themselves — a flat `row[field]` lookup would silently render an empty
 * cell for every nested column.
 *
 * @param row - The row object to read from.
 * @param field - The field path, with `.` separating nested property names.
 * @returns The resolved value, or `undefined` when any segment is missing.
 */
export const resolveFieldData = (row: unknown, field: string): unknown => {
    if (row === null || row === undefined) return undefined;
    if (!field.includes('.')) return (row as Record<string, unknown>)[field];

    return field.split('.').reduce<unknown>((current, segment) => {
        if (current === null || current === undefined) return undefined;
        return (current as Record<string, unknown>)[segment];
    }, row);
};
