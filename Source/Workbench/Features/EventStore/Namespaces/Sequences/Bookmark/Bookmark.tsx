// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { BookmarkTree } from './components/BookmarkTree';
import { useBookmarkNodes } from './hook/useBookmark';
import { EventSequenceQueryFolder } from 'Api/SequenceQueries/Listing/EventSequenceQueryFolder';

export interface BookmarkProps {
    folders: EventSequenceQueryFolder[];
    currentUserSubject?: string;
    onSaveFolder: (name: string, shared: boolean) => Promise<void>;
    onSelectQuery?: (folderId: string, queryId: string) => void;
}

export const Bookmark = ({ folders, currentUserSubject, onSaveFolder, onSelectQuery }: BookmarkProps) => {
    const {
        nodes,
        editingNodeKey,
        expandedKeys,
        setExpandedKeys,
        addNewFolder,
        handleInputChange,
        handleInputKeyDown,
        exitEditMode,
    } = useBookmarkNodes({ folders, currentUserSubject, onSaveFolder });

    return (
        <BookmarkTree
            nodes={nodes}
            editingNodeKey={editingNodeKey}
            expandedKeys={expandedKeys}
            setExpandedKeys={setExpandedKeys}
            addNewFolder={addNewFolder}
            handleInputChange={handleInputChange}
            handleInputKeyDown={handleInputKeyDown}
            exitEditMode={exitEditMode}
            onSelectQuery={onSelectQuery}
        />
    );
};
