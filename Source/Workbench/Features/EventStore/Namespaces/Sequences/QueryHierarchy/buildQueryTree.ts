// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryFolder } from 'Api/SequenceQueries/QueryFolder';
import { SequenceQuery } from 'Api/SequenceQueries/SequenceQuery';
import { SequenceQueryScope } from 'Api/SequenceQueries/SequenceQueryScope';
import { QueryNode } from './QueryNode';
import { QueryNodeKind } from './QueryNodeKind';

/** What separates one folder level from the next in a folder path. */
export const folderSeparator = '/';

/**
 * Builds the identifier a folder or scope node keeps across refreshes.
 * @param scope The scope the node lives under.
 * @param folder The folder path, or empty for the scope root.
 * @returns The identifier.
 */
export const folderNodeId = (scope: SequenceQueryScope, folder: string): string => `${scope}:folder:${folder}`;

/**
 * Builds the identifier a saved query node keeps across refreshes.
 * @param queryId The saved query identifier.
 * @returns The identifier.
 */
export const queryNodeId = (queryId: string): string => `query:${queryId}`;

/**
 * Split a folder path into its segments, ignoring blank ones.
 * @param folder The folder path.
 * @returns The segments.
 */
export const folderSegments = (folder: string): string[] =>
    folder.split(folderSeparator).map(segment => segment.trim()).filter(segment => segment.length > 0);

/**
 * Join a parent folder path and a segment into a folder path.
 * @param parent The parent folder path, or empty for the root of a scope.
 * @param segment The segment to append.
 * @returns The folder path.
 */
export const joinFolder = (parent: string, segment: string): string =>
    parent ? `${parent}${folderSeparator}${segment}` : segment;

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
 * Resolve the folder chain a query hangs off, creating any folder that does not exist yet.
 * @param root The scope node to descend from.
 * @param scope The scope being built.
 * @param folder The folder path to resolve.
 * @returns The node the query should be added to.
 */
const resolveFolder = (root: QueryNode, scope: SequenceQueryScope, folder: string): QueryNode => {
    let current = root;
    let path = '';

    for (const segment of folderSegments(folder)) {
        path = joinFolder(path, segment);
        const id = folderNodeId(scope, path);
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
 * @param folders The folders stored for the hierarchy, including the ones holding nothing yet.
 * @returns The scope roots, each with its folders and queries nested underneath.
 * @remarks
 * Both roots are always returned, even with nothing saved under them, so there is somewhere to drop
 * a first query or folder into. Folders come from two places - the ones stored in their own right
 * and the paths the queries carry - because a query can be filed into a folder name typed straight
 * into the save dialog, without a folder ever having been created for it.
 */
export const buildQueryTree = (
    queries: SequenceQuery[],
    onlyMeLabel: string,
    everyoneLabel: string,
    folders: QueryFolder[] = []): QueryNode[] => {
    const roots: QueryNode[] = [SequenceQueryScope.user, SequenceQueryScope.everyone].map(scope => ({
        id: folderNodeId(scope, ''),
        kind: QueryNodeKind.Scope,
        name: scope === SequenceQueryScope.user ? onlyMeLabel : everyoneLabel,
        scope,
        folder: '',
        children: []
    }));

    const rootFor = (scope: SequenceQueryScope) => roots.find(candidate => candidate.scope === scope);

    for (const folder of folders) {
        const root = rootFor(folder.scope);
        if (root) resolveFolder(root, folder.scope, folder.path);
    }

    for (const query of queries) {
        const root = rootFor(query.scope);
        if (!root) continue;

        resolveFolder(root, query.scope, query.folder ?? '').children.push({
            id: queryNodeId(query.id),
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

/**
 * Collect every folder path that exists within a scope, for offering them as places to file a query.
 * @param queries The saved queries visible to the user.
 * @param scope The scope to collect within.
 * @param folders The folders stored for the hierarchy.
 * @returns The folder paths, sorted.
 */
export const foldersInScope = (
    queries: SequenceQuery[],
    scope: SequenceQueryScope,
    folders: QueryFolder[] = []): string[] => {
    const paths = new Set<string>();

    const addWithAncestors = (folder: string) => {
        let path = '';
        for (const segment of folderSegments(folder)) {
            path = joinFolder(path, segment);
            paths.add(path);
        }
    };

    queries.filter(query => query.scope === scope).forEach(query => addWithAncestors(query.folder ?? ''));
    folders.filter(folder => folder.scope === scope).forEach(folder => addWithAncestors(folder.path));

    return [...paths].sort();
};
