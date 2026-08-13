// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import strings from 'Strings';
import { SequenceQuery } from 'Api/SequenceQueries/SequenceQuery';
import { SequenceQueryScope } from 'Api/SequenceQueries/SequenceQueryScope';
import { buildQueryTree, queryNodeId } from './buildQueryTree';
import { QueryNode } from './QueryNode';
import { QueryNodeKind } from './QueryNodeKind';
import { QueryTreeNode } from './QueryTreeNode';
import './QueryHierarchy.css';

const STORAGE_KEY_EXPANDED = 'cratis.workbench.sequences.hierarchy.expandedIds';
const STORAGE_KEY_WIDTH = 'cratis.workbench.sequences.hierarchy.width';
const MIN_WIDTH = 180;
const MAX_WIDTH = 640;
const DEFAULT_WIDTH = 260;

/**
 * Props for {@link QueryHierarchy}.
 */
export interface QueryHierarchyProps {
    /** The saved queries visible to the user. */
    queries: SequenceQuery[];

    /** The identifier of the query currently being viewed, or null. */
    selectedQueryId: string | null;

    /** Called when a saved query is picked. */
    onOpen: (query: SequenceQuery) => void;

    /** Called to start a new, unsaved query in the given scope and folder. */
    onNewQuery: (scope: SequenceQueryScope, folder: string) => void;

    /** Called to create a folder under the given scope and parent folder. */
    onNewFolder: (scope: SequenceQueryScope, parentFolder: string) => void;

    /** Called when a saved query is renamed from the tree. */
    onRenameQuery: (query: SequenceQuery, name: string) => void;

    /** Called when a saved query is deleted from the tree. */
    onDeleteQuery: (query: SequenceQuery) => void;
}

const readStoredWidth = (): number => {
    const stored = Number(localStorage.getItem(STORAGE_KEY_WIDTH));
    return Number.isFinite(stored) && stored >= MIN_WIDTH ? stored : DEFAULT_WIDTH;
};

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
 * The resizable sidebar listing saved queries as a hierarchy: a root per scope, folders within them,
 * and the queries filed into those folders.
 * @param props The {@link QueryHierarchyProps}.
 * @returns The rendered sidebar.
 */
export const QueryHierarchy = (props: QueryHierarchyProps) => {
    const sequenceStrings = strings.eventStore.namespaces.sequences;

    const roots = useMemo(
        () => buildQueryTree(props.queries, sequenceStrings.scope.onlyMe, sequenceStrings.scope.everyone),
        [props.queries, sequenceStrings.scope.onlyMe, sequenceStrings.scope.everyone]);

    // Both scope roots start open when there is no stored preference, so the queries are visible
    // without having to discover the twisty first.
    const [expandedIds, setExpandedIds] = useState<Set<string>>(() => new Set(
        readStoredExpanded() ?? [
            queryNodeId(SequenceQueryScope.user, ''),
            queryNodeId(SequenceQueryScope.everyone, '')
        ]));

    const [renamingId, setRenamingId] = useState<string | null>(null);
    const [width, setWidth] = useState(readStoredWidth);
    const dragStateRef = useRef<{ startX: number; startWidth: number } | null>(null);

    useEffect(() => {
        localStorage.setItem(STORAGE_KEY_EXPANDED, JSON.stringify([...expandedIds]));
    }, [expandedIds]);

    const toggleExpand = useCallback((id: string) => {
        setExpandedIds(current => {
            const next = new Set(current);
            if (!next.delete(id)) next.add(id);
            return next;
        });
    }, []);

    // Dragging is tracked on the window rather than the handle so the pointer can leave the narrow
    // handle mid-drag without the resize sticking.
    const beginResize = useCallback((event: React.PointerEvent) => {
        dragStateRef.current = { startX: event.clientX, startWidth: width };

        const move = (moveEvent: PointerEvent) => {
            const state = dragStateRef.current;
            if (!state) return;
            const next = Math.min(MAX_WIDTH, Math.max(MIN_WIDTH, state.startWidth + moveEvent.clientX - state.startX));
            setWidth(next);
        };

        const up = () => {
            dragStateRef.current = null;
            window.removeEventListener('pointermove', move);
            window.removeEventListener('pointerup', up);
            setWidth(current => {
                localStorage.setItem(STORAGE_KEY_WIDTH, String(current));
                return current;
            });
        };

        window.addEventListener('pointermove', move);
        window.addEventListener('pointerup', up);
    }, [width]);

    const startRename = useCallback((node: QueryNode) => {
        // Only saved queries can be renamed from here - a folder has no identity of its own beyond the
        // paths of the queries filed under it, so renaming one means rewriting all of them.
        if (node.kind === QueryNodeKind.Query) setRenamingId(node.id);
    }, []);

    const commitRename = useCallback((node: QueryNode, name: string | null) => {
        setRenamingId(null);
        if (name && node.query && name !== node.name) props.onRenameQuery(node.query, name);
    }, [props]);

    return (
        <aside className='query-hierarchy' style={{ width: `${width}px` }}>
            <div className='query-hierarchy__content'>
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
                            selectedId={props.selectedQueryId
                                ? queryNodeId(root.scope, '', props.selectedQueryId)
                                : null}
                            expandedIds={expandedIds}
                            renamingId={renamingId}
                            onToggleExpand={toggleExpand}
                            onSelect={node => node.query && props.onOpen(node.query)}
                            onAddQuery={node => props.onNewQuery(node.scope, node.folder)}
                            onAddFolder={node => props.onNewFolder(node.scope, node.folder)}
                            onStartRename={startRename}
                            onCommitRename={commitRename}
                            onDelete={node => node.query && props.onDeleteQuery(node.query)} />
                    ))}
                </ul>
            </div>

            <div
                className='query-hierarchy__resizer'
                role='separator'
                aria-orientation='vertical'
                onPointerDown={beginResize} />
        </aside>
    );
};
