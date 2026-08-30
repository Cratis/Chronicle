// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SequenceQuery } from 'Features/SequenceQueries';
import { SequenceQueryScope } from 'Features/Concepts/SequenceQueries';
import { QueryNodeKind } from './QueryNodeKind';

/**
 * Represents one row in the query hierarchy.
 */
export interface QueryNode {
    /**
     * Identifies the node within the whole tree.
     * Scope and folder nodes have no identity of their own in the backend, so theirs is derived from
     * the scope and folder path they represent - which is also what makes expansion state survive a
     * refresh, since the same folder always resolves to the same id.
     */
    id: string;

    /** What the node stands for. */
    kind: QueryNodeKind;

    /** The text shown on the row. */
    name: string;

    /** The scope the node lives under. */
    scope: SequenceQueryScope;

    /**
     * The folder path the node sits at - for a folder its own path, for a query the folder holding
     * it, and empty for a scope root.
     */
    folder: string;

    /** The nodes nested under this one. */
    children: QueryNode[];

    /** The saved query, present only on query nodes. */
    query?: SequenceQuery;
}
