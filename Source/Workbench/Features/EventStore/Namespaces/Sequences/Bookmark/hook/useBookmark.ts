// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useCallback, useMemo, useState } from 'react';
import { BookmarkNode, BookmarkOwnerGroup } from '../BookmarkNode';
import { EventSequenceQueryFolder } from 'Api/SequenceQueries/Listing/EventSequenceQueryFolder';

const myQueriesKey = 'group:myQueries';
const sharedQueriesKey = 'group:sharedQueries';
const draftKeyPrefix = 'draft:';
const systemOwner = 'System';

const buildFolderNode = (folder: EventSequenceQueryFolder, ownerGroup: BookmarkOwnerGroup): BookmarkNode => ({
    key: `folder:${folder.folderId.toString()}`,
    label: folder.name,
    icon: 'pi pi-fw pi-inbox',
    ownerGroup,
    isFolder: true,
    folderId: folder.folderId.toString(),
    children: (folder.queries ?? []).map(query => ({
        key: `query:${query.queryId.toString()}`,
        label: query.name,
        icon: 'pi pi-fw pi-file',
        ownerGroup,
        isFolder: false,
        folderId: folder.folderId.toString(),
        queryId: query.queryId.toString(),
    } as BookmarkNode)),
});

export interface UseBookmarkArgs {
    folders: EventSequenceQueryFolder[];
    currentUserSubject?: string;
    onSaveFolder: (name: string, group: BookmarkOwnerGroup) => Promise<void>;
}

export const useBookmarkNodes = ({ folders, currentUserSubject, onSaveFolder }: UseBookmarkArgs) => {
    const [editingNodeKey, setEditingNodeKey] = useState<string | null>(null);
    const [draftName, setDraftName] = useState<string>('');
    const [draftGroup, setDraftGroup] = useState<BookmarkOwnerGroup | null>(null);
    const [expandedKeys, setExpandedKeys] = useState<{ [key: string]: boolean }>({
        [myQueriesKey]: true,
        [sharedQueriesKey]: true,
    });

    const nodes: BookmarkNode[] = useMemo(() => {
        const myFolders = folders.filter(folder => folder.owner !== systemOwner && (currentUserSubject === undefined || folder.owner === currentUserSubject));
        const sharedFolders = folders.filter(folder => folder.owner === systemOwner);

        const myChildren = myFolders.map(folder => buildFolderNode(folder, 'myQueries'));
        const sharedChildren = sharedFolders.map(folder => buildFolderNode(folder, 'sharedQueries'));

        if (draftGroup === 'myQueries') {
            myChildren.push({
                key: `${draftKeyPrefix}myQueries`,
                label: draftName,
                ownerGroup: 'myQueries',
                isFolder: true,
                isDraft: true,
            });
        } else if (draftGroup === 'sharedQueries') {
            sharedChildren.push({
                key: `${draftKeyPrefix}sharedQueries`,
                label: draftName,
                ownerGroup: 'sharedQueries',
                isFolder: true,
                isDraft: true,
            });
        }

        return [
            {
                key: myQueriesKey,
                label: 'My queries',
                icon: 'pi pi-fw pi-user',
                ownerGroup: 'myQueries',
                isFolder: true,
                children: myChildren,
            },
            {
                key: sharedQueriesKey,
                label: 'Shared queries',
                icon: 'pi pi-fw pi-users',
                ownerGroup: 'sharedQueries',
                isFolder: true,
                children: sharedChildren,
            },
        ];
    }, [folders, currentUserSubject, draftGroup, draftName]);

    const addNewFolder = useCallback((group: BookmarkOwnerGroup) => {
        setDraftGroup(group);
        setDraftName('New folder');
        const groupKey = group === 'myQueries' ? myQueriesKey : sharedQueriesKey;
        setExpandedKeys(previous => ({ ...previous, [groupKey]: true }));
        setEditingNodeKey(`${draftKeyPrefix}${group}`);
    }, []);

    const handleInputChange = useCallback((event: React.ChangeEvent<HTMLInputElement>) => {
        setDraftName(event.target.value);
    }, []);

    const handleInputKeyDown = useCallback(async (event: React.KeyboardEvent<HTMLInputElement>) => {
        if (event.key === 'Enter') {
            const name = draftName.trim();
            const group = draftGroup;
            if (name.length > 0 && group !== null) {
                await onSaveFolder(name, group);
            }
            setEditingNodeKey(null);
            setDraftGroup(null);
            setDraftName('');
        } else if (event.key === 'Escape') {
            setEditingNodeKey(null);
            setDraftGroup(null);
            setDraftName('');
        }
    }, [draftName, draftGroup, onSaveFolder]);

    const exitEditMode = useCallback(() => {
        setEditingNodeKey(null);
        setDraftGroup(null);
        setDraftName('');
    }, []);

    return {
        nodes,
        editingNodeKey,
        expandedKeys,
        setExpandedKeys,
        addNewFolder,
        handleInputChange,
        handleInputKeyDown,
        exitEditMode,
    };
};
