// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Button } from 'primereact/button';
import { InputText } from 'primereact/inputtext';
import css from '../Bookmark.module.css';
import { BookmarkNode, BookmarkOwnerGroup } from '../BookmarkNode';

export interface NodeTemplateProps {
    node: BookmarkNode;
    editingNodeKey: string | null;
    addNewFolder: (group: BookmarkOwnerGroup) => void;
    handleInputChange: (event: React.ChangeEvent<HTMLInputElement>) => void;
    handleInputKeyDown: (event: React.KeyboardEvent<HTMLInputElement>) => void;
    exitEditMode: () => void;
}

export const NodeTemplate = ({
    node,
    editingNodeKey,
    addNewFolder,
    handleInputChange,
    handleInputKeyDown,
    exitEditMode,
}: NodeTemplateProps) => {
    const isTopLevelGroup = node.key.startsWith('group:');

    if (editingNodeKey === node.key && node.isDraft === true) {
        return (
            <InputText
                autoFocus
                type='text'
                value={node.label}
                onBlur={exitEditMode}
                onKeyDown={handleInputKeyDown}
                onChange={handleInputChange}
            />
        );
    }

    return (
        <div className={css.itemContainer}>
            <span>{node.label}</span>
            {isTopLevelGroup && (
                <Button
                    unstyled
                    icon='pi pi-plus'
                    className={css.addButton}
                    onClick={() => addNewFolder(node.ownerGroup)}
                />
            )}
        </div>
    );
};
