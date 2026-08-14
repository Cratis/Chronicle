// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useEffect, useRef, useState } from 'react';
import { InputText } from 'primereact/inputtext';
import * as faIcons from 'react-icons/fa6';
import strings from 'Strings';
import { QueryNode } from './QueryNode';
import { QueryNodeKind } from './QueryNodeKind';
import './QueryTreeNode.css';

/**
 * Props for {@link QueryTreeNode}.
 */
export interface QueryTreeNodeProps {
    /** The node the row renders. */
    node: QueryNode;

    /** How deep the node sits, driving the row's indentation. */
    level: number;

    /** The identifier of the selected node, or null when nothing is selected. */
    selectedId: string | null;

    /** The identifiers of the nodes currently expanded. */
    expandedIds: ReadonlySet<string>;

    /** The identifier of the node being renamed, or null when none is. */
    renamingId: string | null;

    /** Called when the row toggles open or closed. */
    onToggleExpand: (id: string) => void;

    /** Called when a query row is picked. */
    onSelect: (node: QueryNode) => void;

    /** Called when the row's add button asks for a new query under it. */
    onAddQuery: (node: QueryNode) => void;

    /** Called when the row's add button asks for a new folder under it. */
    onAddFolder: (node: QueryNode) => void;

    /** Called when the row is double-clicked to start renaming. */
    onStartRename: (node: QueryNode) => void;

    /** Called with the new name, or with null when the rename is abandoned. */
    onCommitRename: (node: QueryNode, name: string | null) => void;

    /** Called when the row's delete button asks to remove the node. */
    onDelete: (node: QueryNode) => void;
}

const iconFor = (node: QueryNode, isExpanded: boolean) => {
    if (node.kind === QueryNodeKind.Query) return <faIcons.FaMagnifyingGlass />;
    if (node.kind === QueryNodeKind.Scope) return <faIcons.FaLayerGroup />;
    return isExpanded ? <faIcons.FaFolderOpen /> : <faIcons.FaFolder />;
};

/**
 * One row of the query hierarchy, plus its children when expanded.
 * @param props The {@link QueryTreeNodeProps}.
 * @returns The rendered row.
 */
export const QueryTreeNode = (props: QueryTreeNodeProps) => {
    const { node, level, selectedId, expandedIds, renamingId } = props;
    const sequenceStrings = strings.eventStore.namespaces.sequences;

    const isQuery = node.kind === QueryNodeKind.Query;
    const isScope = node.kind === QueryNodeKind.Scope;
    const isExpanded = expandedIds.has(node.id);
    const isRenaming = renamingId === node.id;
    const hasChildren = node.children.length > 0;


    const [draft, setDraft] = useState(node.name);
    const inputRef = useRef<HTMLInputElement>(null);

    // The draft starts from the current name every time a rename begins, so abandoning one rename and
    // starting another does not carry the discarded text over.
    useEffect(() => {
        if (isRenaming) {
            setDraft(node.name);
            inputRef.current?.focus();
            inputRef.current?.select();
        }
    }, [isRenaming, node.name]);

    const commit = () => props.onCommitRename(node, draft.trim() || null);

    return (
        <li className='query-tree-node'>
            <div
                className={`query-tree-node__row ${selectedId === node.id ? 'is-selected' : ''}`}
                style={{ paddingLeft: `${level * 0.85 + 0.35}rem` }}
                onClick={() => (isQuery ? props.onSelect(node) : props.onToggleExpand(node.id))}
                onDoubleClick={() => !isRenaming && props.onStartRename(node)}>

                <button
                    type='button'
                    className='query-tree-node__twisty'
                    disabled={!hasChildren}
                    aria-label={node.name}
                    onClick={event => { event.stopPropagation(); props.onToggleExpand(node.id); }}>
                    {hasChildren && (isExpanded ? <faIcons.FaChevronDown /> : <faIcons.FaChevronRight />)}
                </button>

                <span className='query-tree-node__icon'>{iconFor(node, isExpanded)}</span>

                {isRenaming
                    ? <InputText
                        ref={inputRef}
                        className='query-tree-node__rename'
                        value={draft}
                        onChange={event => setDraft(event.target.value)}
                        onBlur={commit}
                        onClick={event => event.stopPropagation()}
                        onDoubleClick={event => event.stopPropagation()}
                        onKeyDown={event => {
                            if (event.key === 'Enter') commit();
                            if (event.key === 'Escape') props.onCommitRename(node, null);
                        }} />
                    : <span className='query-tree-node__name'>{node.name}</span>}

                <span className='query-tree-node__actions'>
                    {!isQuery && (
                        <>
                            <button
                                type='button'
                                title={sequenceStrings.actions.newQuery}
                                aria-label={sequenceStrings.actions.newQuery}
                                onClick={event => { event.stopPropagation(); props.onAddQuery(node); }}>
                                <faIcons.FaPlus />
                            </button>
                            <button
                                type='button'
                                title={sequenceStrings.actions.newFolder}
                                aria-label={sequenceStrings.actions.newFolder}
                                onClick={event => { event.stopPropagation(); props.onAddFolder(node); }}>
                                <faIcons.FaFolderPlus />
                            </button>
                        </>
                    )}
                    {!isScope && (
                        <button
                            type='button'
                            title={isQuery ? sequenceStrings.actions.deleteQuery : sequenceStrings.actions.deleteFolder}
                            aria-label={isQuery ? sequenceStrings.actions.deleteQuery : sequenceStrings.actions.deleteFolder}
                            onClick={event => { event.stopPropagation(); props.onDelete(node); }}>
                            <faIcons.FaTrash />
                        </button>
                    )}
                </span>
            </div>

            {isExpanded && hasChildren && (
                <ul className='query-tree-node__children'>
                    {node.children.map(child => (
                        <QueryTreeNode key={child.id} {...props} node={child} level={level + 1} />
                    ))}
                </ul>
            )}
        </li>
    );
};
