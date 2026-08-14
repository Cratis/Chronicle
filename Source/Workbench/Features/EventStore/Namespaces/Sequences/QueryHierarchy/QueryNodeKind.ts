// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * Represents what a node in the query hierarchy stands for.
 */
export enum QueryNodeKind {
    /** One of the two roots - the queries only the current user sees, or the ones shared with everyone. */
    Scope = 'scope',

    /** A folder grouping queries and further folders within a scope. */
    Folder = 'folder',

    /** A saved query. */
    Query = 'query'
}
