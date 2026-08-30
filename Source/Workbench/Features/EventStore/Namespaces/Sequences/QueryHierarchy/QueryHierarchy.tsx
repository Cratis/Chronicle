// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useCallback, useEffect, useMemo, useState } from 'react';
import strings from 'Strings';
import { QueryFolder } from 'Features/SequenceQueries';
import { SequenceQuery } from 'Features/SequenceQueries';
import { SequenceQueryScope } from 'Features/Concepts/SequenceQueries';
import { buildQueryTree, folderNodeId, queryNodeId } from './buildQueryTree';
import { renamedFolderPath, rewriteFolderPath } from './folderNaming';
import { QueryNode } from './QueryNode';
import { QueryNodeKind } from './QueryNodeKind';
import { QueryTreeNode } from './QueryTreeNode';
import './QueryHierarchy.css';

const STORAGE_KEY_EXPANDED = 'cratis.workbench.sequences.hierarchy.expandedIds';

/**
 * Props for {@link QueryHierarchy}.
 */
export interface QueryHierarchyProps {
    /** The saved queries visible to the user. */
    queries: SequenceQuery[];

    /** The folders stored for the hierarchy, including the ones holding nothing yet. */
    folders: QueryFolder[];

    /** The identifier of the query currently being viewed, or null. */
    selectedQueryId: string | null;

    /** The identifier of the node that should open in rename mode, or null. */
    renamingId: string | null;

    /** Called when the node being renamed changes, including when a rename finishes. */
    onRenamingIdChange: (id: string | null) => void;

    /** Called when a saved query is picked. */
    onOpen: (query: SequenceQuery) => void;

    /** Called to start a new, unsaved query in the given scope and folder. */
    onNewQuery: (scope: SequenceQueryScope, folder: string) => void;

    /** Called to create a folder under the given scope and parent folder. */
    onNewFolder: (scope: SequenceQueryScope, parentFolder: string) => void;

    /** Called when a saved query is renamed from the tree. */
    onRenameQuery: (query: SequenceQuery, name: string) => void;

    /** Called when a folder is renamed from the tree. */
    onRenameFolder: (scope: SequenceQueryScope, folder: string, name: string) => void;

    /** Called when a saved query is deleted from the tree. */
    onDeleteQuery: (query: SequenceQuery) => void;

    /** Called when an empty folder is deleted from the tree. */
    onDeleteFolder: (scope: SequenceQueryScope, folder: string) => void;
}

const readStoredExpanded = (): string[] | null => {
    try {
        const stored = localStorage.getItem(STORAGE_KEY_EXPANDED);
        return stored ? JSON.parse(stored) as string[] : null;
    } catch {
        // A hand-edited or truncated value should fall back to the defaults rather than break the page.
        return null;
    }
};

/**
 * The sidebar listing saved queries as a hierarchy: a root per scope, folders within them, and the
 * queries filed into those folders.
 * @param props The {@link QueryHierarchyProps}.
 * @returns The rendered sidebar.
 */
export const QueryHierarchy = (props: QueryHierarchyProps) => {
    const sequenceStrings = strings.eventStore.namespaces.sequences;

    const roots = useMemo(
        () => buildQueryTree(
            props.queries,
            sequenceStrings.scope.onlyMe,
            sequenceStrings.scope.everyone,
            props.folders),
        [props.queries, props.folders, sequenceStrings.scope.onlyMe, sequenceStrings.scope.everyone]);

    // Both scope roots start open when there is no stored preference, so the queries are visible
    // without having to discover the twisty first.
    const [expandedIds, setExpandedIds] = useState<Set<string>>(() => new Set(
        readStoredExpanded() ?? [
            folderNodeId(SequenceQueryScope.user, ''),
            folderNodeId(SequenceQueryScope.everyone, '')
        ]));

    useEffect(() => {
        localStorage.setItem(STORAGE_KEY_EXPANDED, JSON.stringify([...expandedIds]));
    }, [expandedIds]);

    // A node created from the tree is only reachable once its parent is open, so opening it is part
    // of creating it rather than something the user has to do afterwards.
    useEffect(() => {
        if (!props.renamingId) return;
        setExpandedIds(current => {
            const ancestors = roots.flatMap(root => ancestorIdsOf(root, props.renamingId!));
            if (ancestors.every(id => current.has(id))) return current;

            return new Set([...current, ...ancestors]);
        });
    }, [props.renamingId, roots]);

    const toggleExpand = useCallback((id: string) => {
        setExpandedIds(current => {
            const next = new Set(current);
            if (!next.delete(id)) next.add(id);
            return next;
        });
    }, []);

    const startRename = useCallback((node: QueryNode) => {
        // A scope root is one of the two fixed places a query can live, so its name is not the
        // user's to change.
        if (node.kind !== QueryNodeKind.Scope) props.onRenamingIdChange(node.id);
    }, [props]);

    const commitRename = useCallback((node: QueryNode, name: string | null) => {
        props.onRenamingIdChange(null);
        if (!name || name === node.name) return;

        if (node.kind === QueryNodeKind.Query && node.query) {
            props.onRenameQuery(node.query, name);
            return;
        }

        if (node.kind !== QueryNodeKind.Folder) return;

        // A folder node is identified by its path, so renaming one gives every node at or below it a
        // new identity. Moving the expansion over with them is what keeps the tree from folding up
        // under the user the moment they rename a folder.
        const renamed = renamedFolderPath(node.folder, name);
        setExpandedIds(current => new Set([...current].map(id => rewriteExpandedId(id, node.scope, node.folder, renamed))));
        props.onRenameFolder(node.scope, node.folder, name);
    }, [props]);

    const deleteNode = useCallback((node: QueryNode) => {
        if (node.kind === QueryNodeKind.Query && node.query) {
            props.onDeleteQuery(node.query);
        } else if (node.kind === QueryNodeKind.Folder) {
            props.onDeleteFolder(node.scope, node.folder);
        }
    }, [props]);

    return (
        <aside className='query-hierarchy'>
            <h2 className='query-hierarchy__title'>{sequenceStrings.savedQueries}</h2>

            {props.queries.length === 0 && (
                <p className='query-hierarchy__empty'>{sequenceStrings.noSavedQueries}</p>
            )}

            <ul className='query-hierarchy__tree'>
                {roots.map(root => (
                    <QueryTreeNode
                        key={root.id}
                        node={root}
                        level={0}
                        selectedId={props.selectedQueryId ? queryNodeId(props.selectedQueryId) : null}
                        expandedIds={expandedIds}
                        renamingId={props.renamingId}
                        onToggleExpand={toggleExpand}
                        onSelect={node => node.query && props.onOpen(node.query)}
                        onAddQuery={node => props.onNewQuery(node.scope, node.folder)}
                        onAddFolder={node => props.onNewFolder(node.scope, node.folder)}
                        onStartRename={startRename}
                        onCommitRename={commitRename}
                        onDelete={deleteNode} />
                ))}
            </ul>
        </aside>
    );
};

/**
 * Move an expanded folder identifier over to where its folder was renamed to.
 * @param id The expanded node identifier.
 * @param scope The scope the renamed folder lives under.
 * @param from The folder path before the rename.
 * @param to The folder path after it.
 * @returns The rewritten identifier, or the original when it names something else.
 */
const rewriteExpandedId = (id: string, scope: SequenceQueryScope, from: string, to: string): string => {
    const prefix = `${scope}:folder:`;
    if (!id.startsWith(prefix)) return id;

    return `${prefix}${rewriteFolderPath(id.slice(prefix.length), from, to)}`;
};

/**
 * Collect the identifiers of every node between the root and a node, the root included and the node
 * itself excluded.
 * @param node The node to descend from.
 * @param targetId The identifier to look for.
 * @returns The ancestor identifiers, or an empty array when the target is not underneath.
 */
const ancestorIdsOf = (node: QueryNode, targetId: string): string[] => {
    if (node.id === targetId) return [];

    for (const child of node.children) {
        if (child.id === targetId) return [node.id];

        const beneath = ancestorIdsOf(child, targetId);
        if (beneath.length > 0) return [node.id, ...beneath];
    }

    return [];
};
