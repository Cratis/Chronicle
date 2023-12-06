import { BookmarkNodes, IBookmarkNodes } from './BookmarkNodes';
import { useState, useEffect } from 'react';
import { Tree } from 'primereact/tree';

export const Bookmark = () => {
    const [nodes, setNodes] = useState<IBookmarkNodes[]>([]);

    useEffect(() => {
        BookmarkNodes.getBookmarkNodes().then((data) => setNodes(data));
    }, []);

    return (
        <div>
            <Tree
                filter
                value={nodes}
                filterMode='lenient'
                filterPlaceholder='Search ...'
            />
        </div>
    );
};
