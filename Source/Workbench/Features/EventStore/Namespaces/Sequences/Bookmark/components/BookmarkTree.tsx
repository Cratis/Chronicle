// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { NodeTemplate } from './NodeTemplate';
import { BookmarkNode, BookmarkOwnerGroup } from '../BookmarkNode';
import { Tree, TreeEventNodeEvent } from 'primereact/tree';
import { TreeNode } from 'primereact/treenode';

/* eslint-disable @typescript-eslint/no-explicit-any */

export interface BookmarkTreeProps {
    nodes: BookmarkNode[];
    editingNodeKey: string | null;
    expandedKeys: { [key: string]: boolean };
    setExpandedKeys: React.Dispatch<React.SetStateAction<{ [key: string]: boolean }>>;
    addNewFolder: (group: BookmarkOwnerGroup) => void;
    handleInputChange: (event: React.ChangeEvent<HTMLInputElement>) => void;
    handleInputKeyDown: (event: React.KeyboardEvent<HTMLInputElement>) => void;
    exitEditMode: () => void;
    onSelectQuery?: (folderId: string, queryId: string) => void;
}

export const BookmarkTree = ({
    nodes,
    editingNodeKey,
    expandedKeys,
    setExpandedKeys,
    addNewFolder,
    handleInputChange,
    handleInputKeyDown,
    exitEditMode,
    onSelectQuery,
}: BookmarkTreeProps) => {
    const nodeTemplate = (node: TreeNode) => (
        <NodeTemplate
            node={node as unknown as BookmarkNode}
            editingNodeKey={editingNodeKey}
            addNewFolder={addNewFolder}
            handleInputChange={handleInputChange}
            handleInputKeyDown={handleInputKeyDown}
            exitEditMode={exitEditMode}
        />
    );

    const handleSelect = (event: TreeEventNodeEvent) => {
        const node = event.node as unknown as BookmarkNode;
        if (node.isFolder === false && node.folderId !== undefined && node.queryId !== undefined && onSelectQuery !== undefined) {
            onSelectQuery(node.folderId, node.queryId);
        }
    };

    return (
        <Tree
            filter
            className="h-full w-full"
            value={nodes as unknown as TreeNode[]}
            filterMode='lenient'
            filterPlaceholder='Search'
            nodeTemplate={nodeTemplate}
            expandedKeys={expandedKeys}
            onToggle={(event) => setExpandedKeys(event.value as { [key: string]: boolean })}
            onNodeClick={handleSelect}
            selectionMode='single'
        />
    );
};
