import { BookmarkNodes, IBookmarkNode, IBookmarkNodes } from './BookmarkNodes';
import { useState, useEffect, useCallback } from 'react';
import { EditableFolder } from './EditableFolder';
import { Tree } from 'primereact/tree';

export const Bookmark = () => {
    const [nodes, setNodes] = useState<IBookmarkNodes[]>([]);
    const [editingNodeKey, setEditingNodeKey] = useState<string | null>(null);

    useEffect(() => {
        BookmarkNodes.getBookmarkNodes().then(setNodes);
    }, []);

    const updateNodeLabels = useCallback(
        (nodes: IBookmarkNode[], key: string, newLabel: string): IBookmarkNode[] => {
            return nodes.map((node) => {
                if (node.key === key) {
                    return { ...node, label: newLabel };
                }
                return node.children
                    ? {
                          ...node,
                          children: updateNodeLabels(node.children, key, newLabel),
                      }
                    : node;
            });
        },
        []
    );

    const handleInputChange = useCallback(
        (event: React.ChangeEvent<HTMLInputElement>, editingNode: IBookmarkNode) => {
            setNodes((prevNodes) =>
                updateNodeLabels(prevNodes, editingNode.key, event.target.value)
            );
        },
        [updateNodeLabels]
    );

    const handleInputKeyDown = useCallback(
        (event: React.KeyboardEvent<HTMLInputElement>) => {
            if (event.key === 'Enter') {
                setEditingNodeKey(null);
            }
        },
        []
    );

    const editingMode = useCallback((key: string) => {
        setEditingNodeKey(key);
    }, []);

    const exitEditMode = useCallback(() => {
        setEditingNodeKey(null);
    }, []);

    const nodeTemplate = (node: any) => (
        <EditableFolder
            node={node}
            editMode={editingMode}
            exitEditMode={exitEditMode}
            editingNodeKey={editingNodeKey}
            handleInputChange={handleInputChange}
            handleInputKeyDown={handleInputKeyDown}
        />
    );

    return (
        <div>
            <Tree
                filter
                value={nodes}
                filterMode='lenient'
                nodeTemplate={nodeTemplate}
                filterPlaceholder='Search ...'
            />
        </div>
    );
};
