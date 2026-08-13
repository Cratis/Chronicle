// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SequenceQuery } from 'Api/SequenceQueries/SequenceQuery';
import { SequenceQueryScope } from 'Api/SequenceQueries/SequenceQueryScope';
import { QueryNode } from './QueryNode';
import { QueryNodeKind } from './QueryNodeKind';

const FOLDER_SEPARATOR = '/';

/**
 * Builds the identifier a node keeps across refreshes.
 * @param scope The scope the node lives under.
 * @param folder The folder path, or empty for the scope root.
 * @param queryId The saved query identifier, when the node is a query.
 * @returns The identifier.
 */
export const queryNodeId = (scope: SequenceQueryScope, folder: string, queryId?: string): string =>
    queryId ? `${scope}:query:${queryId}` : `${scope}:folder:${folder}`;

const segmentsOf = (folder: string): string[] =>
    folder.split(FOLDER_SEPARATOR).map(segment => segment.trim()).filter(segment => segment.length > 0);

const byName = (left: QueryNode, right: QueryNode): number => {
    // Folders ahead of queries so the shape of the tree reads before its contents, then by name so
    // the order does not shift when a query is renamed or another one is saved alongside it.
    if (left.kind !== right.kind) return left.kind === QueryNodeKind.Folder ? -1 : 1;
    return left.name.localeCompare(right.name);
};

const sortTree = (nodes: QueryNode[]): QueryNode[] => {
    nodes.sort(byName);
    nodes.forEach(node => sortTree(node.children));
    return nodes;
};

/**
 * Resolves the folder chain a query hangs off, creating any folder that does not exist yet.
 * @param root The scope node to descend from.
 * @param scope The scope being built.
 * @param folder The folder path to resolve.
 * @returns The node the query should be added to.
 */
const resolveFolder = (root: QueryNode, scope: SequenceQueryScope, folder: string): QueryNode => {
    let current = root;
    let path = '';

    for (const segment of segmentsOf(folder)) {
        path = path ? `${path}${FOLDER_SEPARATOR}${segment}` : segment;
        const id = queryNodeId(scope, path);
        let next = current.children.find(child => child.kind === QueryNodeKind.Folder && child.id === id);

        if (!next) {
            next = {
                id,
                kind: QueryNodeKind.Folder,
                name: segment,
                scope,
                folder: path,
                children: []
            };
            current.children.push(next);
        }

        current = next;
    }

    return current;
};

/**
 * Builds the two-rooted query hierarchy from the saved queries.
 * @param queries The saved queries visible to the user.
 * @param onlyMeLabel The label for the root holding the user's own queries.
 * @param everyoneLabel The label for the root holding the queries shared with everyone.
 * @returns The scope roots, each with its folders and queries nested underneath.
 * @remarks
 * Both roots are always returned, even with nothing saved under them, so there is somewhere to drop
 * a first query or folder into.
 */
export const buildQueryTree = (
    queries: SequenceQuery[],
    onlyMeLabel: string,
    everyoneLabel: string): QueryNode[] => {
    const roots: QueryNode[] = [SequenceQueryScope.user, SequenceQueryScope.everyone].map(scope => ({
        id: queryNodeId(scope, ''),
        kind: QueryNodeKind.Scope,
        name: scope === SequenceQueryScope.user ? onlyMeLabel : everyoneLabel,
        scope,
        folder: '',
        children: []
    }));

    for (const query of queries) {
        const root = roots.find(candidate => candidate.scope === query.scope);
        if (!root) continue;

        resolveFolder(root, query.scope, query.folder ?? '').children.push({
            id: queryNodeId(query.scope, query.folder ?? '', query.id),
            kind: QueryNodeKind.Query,
            name: query.name,
            scope: query.scope,
            folder: query.folder ?? '',
            children: [],
            query
        });
    }

    roots.forEach(root => sortTree(root.children));

    return roots;
};
