import { InputText } from 'primereact/inputtext';
import { IBookmarkNode } from './BookmarkNodes';
import React from 'react';

interface EditableFolderProps {
    node: IBookmarkNode;
    editMode: (key: string) => void;
    exitEditMode: () => void;
    editingNodeKey: string | null;
    handleInputChange: (
        event: React.ChangeEvent<HTMLInputElement>,
        node: IBookmarkNode
    ) => void;
    handleInputKeyDown: (event: React.KeyboardEvent<HTMLInputElement>) => void;
}

export const EditableFolder = ({
    node,
    editMode,
    exitEditMode,
    editingNodeKey,
    handleInputChange,
    handleInputKeyDown,
}: EditableFolderProps) => {
    return editingNodeKey === node.key && node.children ? (
        <InputText
            type='text'
            autoFocus
            value={node.label}
            onKeyDown={handleInputKeyDown}
            onBlur={exitEditMode}
            onChange={(e) => handleInputChange(e, node)}
        />
    ) : (
        <span onDoubleClick={() => editMode(node.key)}>{node.label}</span>
    );
};
