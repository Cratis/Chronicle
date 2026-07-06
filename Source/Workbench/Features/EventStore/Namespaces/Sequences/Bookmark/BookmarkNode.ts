// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * Source classification for a bookmark tree node.
 * `myQueries` and `sharedQueries` are the two top-level group identifiers.
 */
export type BookmarkOwnerGroup = 'myQueries' | 'sharedQueries';

/**
 * Node in the saved-queries tree rendered by the Bookmark sidebar.
 */
export interface BookmarkNode {
    key: string;
    label: string;
    icon?: string;
    ownerGroup: BookmarkOwnerGroup;
    isFolder: boolean;
    isDraft?: boolean;
    folderId?: string;
    queryId?: string;
    children?: BookmarkNode[];
}
