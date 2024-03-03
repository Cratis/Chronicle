/* Copyright (c) Aksio Insurtech. All rights reserved.
   Licensed under the MIT license. See LICENSE file in the project root for full license information. */

import { BookmarkTree } from './components/BookmarkTree';
import { useBookmarkNodes } from './hook/useBookmark';

export const Bookmark = () => {
    const {
        nodes,
        editMode,
        deleteNode,
        expandedKeys,
        addNewFolder,
        exitEditMode,
        editingNodeKey,
        setExpandedKeys,
        handleInputChange,
        handleInputKeyDown,
    } = useBookmarkNodes();

    return (
        <BookmarkTree
            nodes={nodes}
            editMode={editMode}
            deleteNode={deleteNode}
            exitEditMode={exitEditMode}
            expandedKeys={expandedKeys}
            addNewFolder={addNewFolder}
            editingNodeKey={editingNodeKey}
            setExpandedKeys={setExpandedKeys}
            handleInputChange={handleInputChange}
            handleInputKeyDown={handleInputKeyDown}
        />
    );
};
