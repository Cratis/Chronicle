/* Copyright (c) Aksio Insurtech. All rights reserved.
   Licensed under the MIT license. See LICENSE file in the project root for full license information. */

import { InputText } from 'primereact/inputtext';
import { IBookmarkNode } from '../TestData';
import { Button } from 'primereact/button';
import css from '../Bookmark.module.css';
import React from 'react';

interface EditableFolderProps {
    node: IBookmarkNode;
    secondElement: boolean;
    exitEditMode: () => void;
    editingNodeKey: string | null;
    editMode: (key: string) => void;
    addNewFolder: (nodeKey: string) => void;
    handleInputChange: (
        evt: React.ChangeEvent<HTMLInputElement>,
        node: IBookmarkNode
    ) => void;
    handleInputKeyDown: (evt: React.KeyboardEvent<HTMLInputElement>) => void;
}

export const EditableFolder = (props: EditableFolderProps) => {
    const {
        node,
        editMode,
        secondElement,
        addNewFolder,
        exitEditMode,
        editingNodeKey,
        handleInputChange,
        handleInputKeyDown,
    } = props;
    return (
        <>
            {editingNodeKey === node.key && node.children ? (
                <InputText
                    type='text'
                    autoFocus
                    value={node.label}
                    onBlur={exitEditMode}
                    onKeyDown={handleInputKeyDown}
                    onChange={(evt) => handleInputChange(evt, node)}
                />
            ) : (
                <span onDoubleClick={() => editMode(node?.key)}>{node.label}</span>
            )}

            {secondElement && (
                <Button
                    rounded
                    size='small'
                    icon='pi pi-plus'
                    className={css.addButton}
                    onClick={() => addNewFolder(node?.key)}
                />
            )}
        </>
    );
};
