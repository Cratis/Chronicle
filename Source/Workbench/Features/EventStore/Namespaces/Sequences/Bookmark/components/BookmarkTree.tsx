/* Copyright (c) Cratis. All rights reserved.
   Licensed under the MIT license. See LICENSE file in the project root for full license information. */

import { NodeTemplate } from './NodeTemplate';
import { IBookmarkNode } from '../TestData';
import { Tree } from 'primereact/tree';

export interface IBookmarkTreeProps {
    nodes: IBookmarkNode[];
    exitEditMode: () => void;
    editingNodeKey: string | null;
    editMode: (key: string) => void;
    deleteNode: (nodeKey: string) => void;
    expandedKeys: { [key: string]: boolean };
    addNewFolder: (parentNodeKey: string) => void;
    handleInputChange: (
        evt: React.ChangeEvent<HTMLInputElement>,
        editingNode: IBookmarkNode
    ) => void;
    handleInputKeyDown: (evt: React.KeyboardEvent<HTMLInputElement>) => void;
    setExpandedKeys: React.Dispatch<React.SetStateAction<{ [key: string]: boolean }>>;
}

export const BookmarkTree = (props: IBookmarkTreeProps) => {
    const {
        nodes,
        editMode,
        deleteNode,
        exitEditMode,
        expandedKeys,
        addNewFolder,
        editingNodeKey,
        setExpandedKeys,
        handleInputChange,
        handleInputKeyDown,
    } = props;

    const nodeTemplate = (node: any) => (
        <NodeTemplate
            node={node}
            editMode={editMode}
            deleteNode={deleteNode}
            exitEditMode={exitEditMode}
            addNewFolder={addNewFolder}
            editingNodeKey={editingNodeKey}
            handleInputChange={handleInputChange}
            handleInputKeyDown={handleInputKeyDown}
        />
    );

    return (
        <Tree
            filter
            className="h-full w-full"
            value={nodes}
            filterMode='lenient'
            filterPlaceholder='Search'
            nodeTemplate={nodeTemplate}
            expandedKeys={expandedKeys}
            onToggle={(e) => setExpandedKeys(e.value)}
        />
    );
};
