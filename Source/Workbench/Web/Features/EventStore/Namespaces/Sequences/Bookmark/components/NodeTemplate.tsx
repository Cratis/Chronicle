// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { EditableFolder } from './EditableFolder';
import { IBookmarkNode } from '../TestData';

export interface INodeTemplateProps {
    node: IBookmarkNode;
    exitEditMode: () => void;
    editingNodeKey: string | null;
    editMode: (key: string) => void;
    deleteNode: (nodeKey: string) => void;
    addNewFolder: (parentNodeKey: string) => void;
    handleInputChange: (
        evt: React.ChangeEvent<HTMLInputElement>,
        editingNode: IBookmarkNode
    ) => void;
    handleInputKeyDown: (evt: React.KeyboardEvent<HTMLInputElement>) => void;
}

export const NodeTemplate = (props: INodeTemplateProps) => {
    const {
        node,
        editMode,
        deleteNode,
        exitEditMode,
        addNewFolder,
        editingNodeKey,
        handleInputChange,
        handleInputKeyDown,
    } = props;

    const secondElement = node.key === '1';
    const isTopLevelNode = !node.key.includes('-');
    return (
        <EditableFolder
            node={node}
            editMode={editMode}
            deleteNode={deleteNode}
            addNewFolder={addNewFolder}
            exitEditMode={exitEditMode}
            secondElement={secondElement}
            editingNodeKey={editingNodeKey}
            showDeleteIcon={!isTopLevelNode}
            handleInputChange={handleInputChange}
            handleInputKeyDown={handleInputKeyDown}
        />
    );
};
