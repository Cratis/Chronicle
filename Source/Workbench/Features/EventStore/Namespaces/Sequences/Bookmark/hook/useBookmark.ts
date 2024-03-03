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
        (
            nodes: IBookmarkNode[],
            key: string,
            newValues: Partial<IBookmarkNode>
        ): IBookmarkNode[] => {
            return nodes.map((node) => {
                if (node.key === key) {
                    return { ...node, ...newValues };
                }
                if (node.children && node.children.length > 0) {
                    return {
                        ...node,
                        children: updateNodeLabels(node.children, key, newValues),
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
                updateNodeLabels(prevNodes, editingNode.key, { label: evt.target.value })
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
    const deleteNode = useCallback((nodeKeyToDelete: string): void => {
        setNodes((prevNodes: IBookmarkNode[]) => {
            const removeNodeAndChildren = (
                nodes: IBookmarkNode[],
                key: string
            ): IBookmarkNode[] => {
                return nodes
                    .filter((node) => node.key !== key)
                    .map((node) => {
                        if (node.children) {
                            return {
                                ...node,
                                children: removeNodeAndChildren(node.children, key),
                            };
                        }
                        return node;
                    });
            };

            return removeNodeAndChildren(prevNodes, nodeKeyToDelete);
        });
    }, []);

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
        setExpandedKeys((prevExpandedKeys) => ({
            ...prevExpandedKeys,
            [parentNodeKey]: true,
        }));

        setEditingNodeKey(newFolderKey);
    }, []);

    return {
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
    };
};
