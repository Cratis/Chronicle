// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { BookmarkTree } from './components/BookmarkTree';
import { BookmarkOwnerGroup } from './BookmarkNode';
import { useBookmarkNodes } from './hook/useBookmark';
import { EventSequenceQueryFolder } from 'Api/SequenceQueries/Listing/EventSequenceQueryFolder';

export interface BookmarkProps {
    folders: EventSequenceQueryFolder[];
    currentUserSubject?: string;
    onAddFolder: (group: BookmarkOwnerGroup, name: string) => Promise<void>;
    onAddQuery: (group: BookmarkOwnerGroup) => void;
    onSelectQuery?: (folderId: string, queryId: string) => void;
}

export const Bookmark = ({ folders, currentUserSubject, onAddFolder, onAddQuery, onSelectQuery }: BookmarkProps) => {
    const {
        nodes,
        editingNodeKey,
        expandedKeys,
        setExpandedKeys,
        addNewFolder,
        handleInputChange,
        handleInputKeyDown,
        exitEditMode,
    } = useBookmarkNodes({ folders, currentUserSubject, onSaveFolder: (name, group) => onAddFolder(group, name) });

    return (
        <BookmarkTree
            nodes={nodes}
            editingNodeKey={editingNodeKey}
            expandedKeys={expandedKeys}
            setExpandedKeys={setExpandedKeys}
            addNewFolder={addNewFolder}
            addNewQuery={onAddQuery}
            handleInputChange={handleInputChange}
            handleInputKeyDown={handleInputKeyDown}
            exitEditMode={exitEditMode}
            onSelectQuery={onSelectQuery}
        />
    );
};
