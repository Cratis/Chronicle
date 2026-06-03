// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Button } from 'primereact/button';
import { InputText } from 'primereact/inputtext';
import { Menu } from 'primereact/menu';
import { useRef } from 'react';
import css from '../Bookmark.module.css';
import { BookmarkNode, BookmarkOwnerGroup } from '../BookmarkNode';

export interface NodeTemplateProps {
    node: BookmarkNode;
    editingNodeKey: string | null;
    addNewFolder: (group: BookmarkOwnerGroup) => void;
    addNewQuery: (group: BookmarkOwnerGroup) => void;
    handleInputChange: (event: React.ChangeEvent<HTMLInputElement>) => void;
    handleInputKeyDown: (event: React.KeyboardEvent<HTMLInputElement>) => void;
    exitEditMode: () => void;
}

export const NodeTemplate = ({
    node,
    editingNodeKey,
    addNewFolder,
    addNewQuery,
    handleInputChange,
    handleInputKeyDown,
    exitEditMode,
}: NodeTemplateProps) => {
    const isTopLevelGroup = node.key.startsWith('group:');
    const menuRef = useRef<Menu>(null);

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
                <>
                    <Menu
                        ref={menuRef}
                        popup
                        model={[
                            {
                                label: 'Query',
                                icon: 'pi pi-file',
                                command: () => addNewQuery(node.ownerGroup),
                            },
                            {
                                label: 'Folder',
                                icon: 'pi pi-folder',
                                command: () => addNewFolder(node.ownerGroup),
                            },
                        ]} />
                    <Button
                        unstyled
                        icon='pi pi-plus'
                        className={css.addButton}
                        onClick={(event) => {
                            event.stopPropagation();
                            menuRef.current?.toggle(event);
                        }}
                    />
                </>
            )}
        </div>
    );
};
