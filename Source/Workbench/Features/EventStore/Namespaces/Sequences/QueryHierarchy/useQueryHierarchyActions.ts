// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useCallback, useState } from 'react';
import { DialogButtons, DialogResult, useConfirmationDialog } from '@cratis/arc.react/dialogs';
import strings from 'Strings';
import { DeleteSequenceQuery } from 'Api/SequenceQueries/DeleteSequenceQuery';
import { DeleteSequenceQueryFolder } from 'Api/SequenceQueries/DeleteSequenceQueryFolder';
import { QueryFolder } from 'Api/SequenceQueries/QueryFolder';
import { SaveSequenceQueryFolder } from 'Api/SequenceQueries/SaveSequenceQueryFolder';
import { SequenceQuery } from 'Api/SequenceQueries/SequenceQuery';
import { SequenceQueryScope } from 'Api/SequenceQueries/SequenceQueryScope';
import { saveSequenceQuery } from '../QueryEditor/saveSequenceQuery';
import { SequenceQueryState, toSequenceQueryState } from '../QueryEditor/SequenceQueryState';
import { folderNodeId, foldersInScope, queryNodeId } from './buildQueryTree';
import { renamedFolderPath, rewriteFolderPath, uniqueFolderPath } from './folderNaming';

/**
 * Determine whether a folder path sits at, or underneath, another one.
 * @param path The path to test.
 * @param folder The folder that is being deleted or moved.
 * @returns True when the path goes with it.
 */
const isAtOrBelow = (path: string, folder: string) => path === folder || path.startsWith(`${folder}/`);

const deleteStoredQuery = async (eventStore: string, id: string) => {
    const command = new DeleteSequenceQuery();
    command.eventStore = eventStore;
    command.id = id;

    return (await command.execute()).isSuccess;
};

const deleteStoredFolder = async (eventStore: string, id: string) => {
    const command = new DeleteSequenceQueryFolder();
    command.eventStore = eventStore;
    command.id = id;

    return (await command.execute()).isSuccess;
};

const saveFolder = async (eventStore: string, folder: { id: string; scope: SequenceQueryScope; namespace: string; path: string }) => {
    const command = new SaveSequenceQueryFolder();
    command.eventStore = eventStore;
    command.id = folder.id;
    command.scope = folder.scope;
    command.namespace = folder.namespace;
    command.path = folder.path;

    const result = await command.execute();
    return result.isSuccess;
};

/**
 * Everything the hierarchy can do to the queries and folders it shows.
 * @param eventStore The event store the queries belong to.
 * @param namespace The namespace being viewed.
 * @param queries The saved queries visible to the user.
 * @param folders The stored folders visible to the user.
 * @param onChanged Called after the stored queries or folders changed, so they can be re-read.
 * @param onQueryClosed Called with the identifier of a query that no longer exists.
 * @param onPersisted Called with a change already written back, so open tabs can follow it.
 * @returns The handlers the hierarchy binds to, and the node it wants renamed.
 */
export const useQueryHierarchyActions = (
    eventStore: string,
    namespace: string,
    queries: SequenceQuery[],
    folders: QueryFolder[],
    onChanged: () => void,
    onQueryClosed: (id: string) => void,
    onPersisted: (change: (state: SequenceQueryState) => SequenceQueryState) => void) => {
    const sequenceStrings = strings.eventStore.namespaces.sequences;
    const [renamingId, setRenamingId] = useState<string | null>(null);
    const [showConfirmation] = useConfirmationDialog();

    const newFolder = useCallback(async (scope: SequenceQueryScope, parentFolder: string) => {
        const path = uniqueFolderPath(parentFolder, sequenceStrings.newFolder, foldersInScope(queries, scope, folders));

        // Named straight after it appears, so the folder is stored before the rename rather than
        // only once the user settles on a name - closing the page mid-rename still leaves it there.
        if (!await saveFolder(eventStore, { id: crypto.randomUUID(), scope, namespace, path })) return;

        setRenamingId(folderNodeId(scope, path));
        onChanged();
    }, [queries, folders, eventStore, namespace, onChanged, sequenceStrings.newFolder]);

    const deleteFolder = useCallback(async (scope: SequenceQueryScope, folder: string) => {
        // Deleting a folder takes everything filed under it - nested folders and the queries in
        // them - so the count is spelled out before anything is removed.
        const doomedFolders = folders.filter(candidate =>
            candidate.scope === scope && isAtOrBelow(candidate.path, folder));
        const doomedQueries = queries.filter(query =>
            query.scope === scope && isAtOrBelow(query.folder ?? '', folder));

        const confirmed = await showConfirmation(
            sequenceStrings.dialogs.deleteFolder.title,
            doomedQueries.length === 0
                ? sequenceStrings.dialogs.deleteFolder.empty.replace('{folder}', folder)
                : sequenceStrings.dialogs.deleteFolder.withQueries
                    .replace('{folder}', folder)
                    .replace('{count}', doomedQueries.length.toString()),
            DialogButtons.YesNo);
        if (confirmed !== DialogResult.Yes) return;

        await Promise.all(doomedQueries.map(query => deleteStoredQuery(eventStore, query.id)));
        await Promise.all(doomedFolders.map(candidate => deleteStoredFolder(eventStore, candidate.id)));

        doomedQueries.forEach(query => onQueryClosed(query.id));
        onChanged();
    }, [folders, queries, eventStore, onChanged, onQueryClosed, showConfirmation, sequenceStrings]);

    const renameFolder = useCallback(async (scope: SequenceQueryScope, folder: string, name: string) => {
        const renamed = renamedFolderPath(folder, name);
        if (renamed === folder) return;

        // The folder and everything filed at or below it carry the path being renamed - stored
        // folders and queries alike - so all of them are written back under the new one.
        const movedFolders = folders.filter(candidate =>
            candidate.scope === scope && rewriteFolderPath(candidate.path, folder, renamed) !== candidate.path);
        const movedQueries = queries.filter(query =>
            query.scope === scope && rewriteFolderPath(query.folder ?? '', folder, renamed) !== (query.folder ?? ''));

        if (movedFolders.length === 0 && movedQueries.length === 0) return;

        await Promise.all([
            ...movedFolders.map(candidate => saveFolder(eventStore, {
                id: candidate.id,
                scope: candidate.scope,
                namespace: candidate.namespace,
                path: rewriteFolderPath(candidate.path, folder, renamed)
            })),
            ...movedQueries.map(query => saveSequenceQuery(
                { ...toSequenceQueryState(query), folder: rewriteFolderPath(query.folder ?? '', folder, renamed) },
                eventStore))
        ]);

        onPersisted(state => (state.scope === scope
            ? { ...state, folder: rewriteFolderPath(state.folder, folder, renamed) }
            : state));
        onChanged();
    }, [queries, folders, eventStore, onChanged, onPersisted]);

    const renameQuery = useCallback(async (query: SequenceQuery, name: string) => {
        if (!await saveSequenceQuery({ ...toSequenceQueryState(query), name }, eventStore)) return;

        onPersisted(state => (state.id === query.id ? { ...state, name } : state));
        onChanged();
    }, [eventStore, onChanged, onPersisted]);

    const deleteQuery = useCallback(async (query: SequenceQuery) => {
        const confirmed = await showConfirmation(
            sequenceStrings.dialogs.deleteQuery.title,
            sequenceStrings.dialogs.deleteQuery.message.replace('{name}', query.name),
            DialogButtons.YesNo);
        if (confirmed !== DialogResult.Yes) return;

        if (!await deleteStoredQuery(eventStore, query.id)) return;

        onQueryClosed(query.id);
        onChanged();
    }, [eventStore, onChanged, onQueryClosed, showConfirmation, sequenceStrings]);

    const startRenamingQuery = useCallback((id: string) => setRenamingId(queryNodeId(id)), []);

    return {
        renamingId,
        setRenamingId,
        newFolder,
        deleteFolder,
        renameFolder,
        renameQuery,
        deleteQuery,
        startRenamingQuery
    };
};
