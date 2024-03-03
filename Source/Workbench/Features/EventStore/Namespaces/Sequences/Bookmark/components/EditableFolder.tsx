/* Copyright (c) Cratis. All rights reserved.
   Licensed under the MIT license. See LICENSE file in the project root for full license information. */

import { ConfirmPopup } from 'primereact/confirmpopup';
import { InputText } from 'primereact/inputtext';
import { IBookmarkNode } from '../TestData';
import { Button } from 'primereact/button';
import css from '../Bookmark.module.css';
import React, { useState } from 'react';

interface EditableFolderProps {
    node: IBookmarkNode;
    secondElement: boolean;
    showDeleteIcon: boolean;
    exitEditMode: () => void;
    editingNodeKey: string | null;
    editMode: (key: string) => void;
    deleteNode: (nodeKey: string) => void;
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
        deleteNode,
        addNewFolder,
        exitEditMode,
        secondElement,
        showDeleteIcon,
        editingNodeKey,
        handleInputChange,
        handleInputKeyDown,
    } = props;

    const [isConfirmVisible, setIsConfirmVisible] = useState(false);
    const [nodeToDelete, setNodeToDelete] = useState<string>();

    return (
        <>
            {editingNodeKey === node.key ? (
                <InputText
                    autoFocus
                    type='text'
                    value={node.label}
                    onBlur={exitEditMode}
                    onKeyDown={handleInputKeyDown}
                    onChange={(evt) => handleInputChange(evt, node)}
                />
            ) : (
                <div className={css.itemContainer}>
                    <span onDoubleClick={() => editMode(node?.key)}>{node.label}</span>
                    {secondElement && (
                        <Button
                            unstyled
                            icon='pi pi-plus'
                            className={css.addButton}
                            onClick={() => addNewFolder(node?.key)}
                        />
                    )}
                    {showDeleteIcon && (
                        <Button
                            unstyled
                            icon='pi pi-trash'
                            className={css.deleteButton}
                            id={`delete-btn-${node.key}`}
                            onClick={(e) => {
                                setNodeToDelete(node.key);
                                setIsConfirmVisible(true);
                                e.persist(); // Needed to keep the event around for the ConfirmPopup
                            }}
                        />
                    )}
                </div>
            )}
            <ConfirmPopup
                target={
                    document.getElementById(`delete-btn-${nodeToDelete}`) || undefined
                }
                visible={isConfirmVisible}
                onHide={() => setIsConfirmVisible(false)}
                message='Are you sure you want to delete this item?'
                accept={() => {
                    deleteNode(nodeToDelete!);
                    setIsConfirmVisible(false);
                }}
                reject={() => setIsConfirmVisible(false)}
            />
        </>
    );
};
