// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { type StorageAuthorizationDiagnostic } from 'Features/Diagnostics';

/**
 * Gets the databases the storage user was refused on, each once, in the order they were first reported.
 * An empty result means no authorization failure is known, so the caller keeps its generic text.
 * @param diagnostics The recorded authorization failures.
 * @returns The distinct database names.
 */
export const unauthorizedDatabases = (diagnostics: StorageAuthorizationDiagnostic[]): string[] =>
    [...new Set(diagnostics.map(_ => _.database))];

/**
 * Describes one refused operation by filling a template.
 * @param diagnostic The refused operation.
 * @param template The text, with {operation}, {database} and {collection} placeholders.
 * @returns The description.
 */
export const describeStorageAuthorizationFailure = (diagnostic: StorageAuthorizationDiagnostic, template: string): string =>
    template
        .replace(/\{operation\}/g, () => diagnostic.operation)
        .replace(/\{database\}/g, () => diagnostic.database)
        .replace(/\{collection\}/g, () => diagnostic.collection);
