/* Copyright (c) Aksio Insurtech. All rights reserved.
   Licensed under the MIT license. See LICENSE file in the project root for full license information. */

import { useState, useEffect, useCallback } from 'react';
import { BookmarkNodes, IBookmarkNode } from '../TestData';

export const useBookmarkNodes = () => {
    const [nodes, setNodes] = useState<IBookmarkNode[]>([]);
    const [editingNodeKey, setEditingNodeKey] = useState<string | null>(null);
    const [expandedKeys, setExpandedKeys] = useState<{ [key: string]: boolean }>({});

    useEffect(() => {
        const fetchNodes = async () => {
            try {
                const nodesData = await BookmarkNodes.getBookmarkNodes();
                setNodes(nodesData);
            } catch (error) {
                console.error('Error fetching bookmark nodes:', error);
            }
        };

        fetchNodes();
    }, []);
    const updateNodeLabels = useCallback(
        (nodes: IBookmarkNode[], key: string, newLabel: string): IBookmarkNode[] => {
            return nodes.map((node) => {
                if (node.key === key) {
                    return { ...node, label: newLabel };
                }
                if (node.children) {
                    return {
                        ...node,
                        children: updateNodeLabels(node.children, key, newLabel),
                    };
                }
                return node;
            });
        },
        []
    );

    const handleInputChange = useCallback(
        (evt: React.ChangeEvent<HTMLInputElement>, editingNode: IBookmarkNode) => {
            setNodes((prevNodes) =>
                updateNodeLabels(prevNodes, editingNode.key, evt.target.value)
            );
        },
        [updateNodeLabels]
    );

    const handleInputKeyDown = useCallback(
        (evt: React.KeyboardEvent<HTMLInputElement>) => {
            if (evt.key === 'Enter') {
                setEditingNodeKey(null);
            }
        },
        []
    );

    const editMode = useCallback((key: string) => {
        setEditingNodeKey(key);
    }, []);

    const exitEditMode = useCallback(() => {
        setEditingNodeKey(null);
    }, []);


    const addNewFolder = useCallback((parentNodeKey: string) => {
        const newFolderKey = 'new_' + Math.random().toString();

        const newFolder: IBookmarkNode = {
            key: newFolderKey,
            label: 'New Folder',
            icon: 'pi pi-fw pi-inbox',
            data: 'data goes here',
            children: [],
        };
        setNodes((prevNodes) => {
            const addFolderToNode = (nodes: IBookmarkNode[]): IBookmarkNode[] =>
                nodes.map((node) =>
                    node.key === parentNodeKey
                        ? { ...node, children: [...(node.children || []), newFolder] }
                        : node.children
                            ? { ...node, children: addFolderToNode(node.children) }
                            : node
                );

            return addFolderToNode(prevNodes);
        });
        setExpandedKeys(prevExpandedKeys => ({
            ...prevExpandedKeys,
            [parentNodeKey]: true
        }));

        setEditingNodeKey(newFolderKey);
    }, []);

    return { nodes, addNewFolder, expandedKeys, setExpandedKeys, editingNodeKey, editMode, exitEditMode, handleInputChange, handleInputKeyDown };
};
