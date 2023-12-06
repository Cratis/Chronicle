import { InputText } from 'primereact/inputtext';
import { IBookmarkNode } from './BookmarkNodes';
import React from 'react';

interface EditableFolderProps {
    node: IBookmarkNode;
    editingNodeKey: string | null;
    handleDoubleClick: (node: IBookmarkNode) => void;
    handleInputChange: (
        event: React.ChangeEvent<HTMLInputElement>,
        node: IBookmarkNode
    ) => void;
    handleInputKeyDown: (event: React.KeyboardEvent<HTMLInputElement>) => void;
}

export const EditableFolder = ({
    node,
    editingNodeKey,
    handleInputChange,
    handleInputKeyDown,
    handleDoubleClick,
}: EditableFolderProps) => {
    return editingNodeKey === node.key && node.children ? (
        <InputText
            type='text'
            autoFocus
            value={node.label}
            onBlur={() => handleDoubleClick(node)}
            onChange={(e) => handleInputChange(e, node)}
            onKeyDown={handleInputKeyDown}
        />
    ) : (
        <span onDoubleClick={() => handleDoubleClick(node)}>{node.label}</span>
    );
};
