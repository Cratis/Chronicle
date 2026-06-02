// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { observable } from 'mobx';
import { injectable } from 'tsyringe';
import { Guid } from '@cratis/fundamentals';
import { QueryDefinition } from './QueryDefinition';
import { AddEventSequenceQuery } from 'Api/SequenceQueries/Adding/AddEventSequenceQuery';
import { AddEventSequenceQueryFolder } from 'Api/SequenceQueries/Adding/AddEventSequenceQueryFolder';
import { AddEventSequenceQueryFolderForUser } from 'Api/SequenceQueries/Adding/AddEventSequenceQueryFolderForUser';
import { EventSequenceQueryFolder } from 'Api/SequenceQueries/Listing/EventSequenceQueryFolder';
import { SequenceQueryFilter } from 'Api/SequenceQueries/SequenceQueryFilter';

/**
 * MVVM view model for the Sequences page. Commands and queries are taken as dependencies through the
 * methods rather than imported in component code, keeping the view itself stateless about backend wiring.
 */
@injectable()
export class SequencesViewModel {

    private _folders: EventSequenceQueryFolder[] = observable.array([]);
    private _queries: QueryDefinition[] = observable.array([]);
    private _activeTabIndex: number = 0;
    private _currentQuery?: QueryDefinition;

    get folders(): EventSequenceQueryFolder[] {
        return this._folders;
    }

    get queries(): QueryDefinition[] {
        return this._queries;
    }

    get currentQuery(): QueryDefinition | undefined {
        return this._currentQuery;
    }

    set currentQuery(query: QueryDefinition | undefined) {
        this._currentQuery = query;
    }

    get activeTabIndex(): number {
        return this._activeTabIndex;
    }

    set activeTabIndex(index: number) {
        this._activeTabIndex = index;
        this._currentQuery = this._queries[index];
    }

    get hasChanges(): boolean {
        return this._queries.some(query => query.isUnsaved === true);
    }

    setFoldersFromApi(folders: EventSequenceQueryFolder[]) {
        this._folders = observable.array(folders);
        const flattened = folders.flatMap(folder => (folder.queries ?? []).map(query => ({
            id: query.queryId?.toString(),
            name: query.name,
            eventSequenceId: query.eventSequenceId,
            eventSourceId: query.filter?.eventSourceId ?? undefined,
            eventTypes: query.filter?.eventTypes ?? [],
            startTime: query.filter?.startTime ? new Date(query.filter.startTime as unknown as string) : undefined,
            endTime: query.filter?.endTime ? new Date(query.filter.endTime as unknown as string) : undefined,
            folderId: folder.folderId?.toString(),
            isUnsaved: false,
        } as QueryDefinition)));
        this._queries = observable.array(flattened);
        if (flattened.length > 0 && this._currentQuery === undefined) {
            this._currentQuery = this._queries[0];
            this._activeTabIndex = 0;
        }
    }

    async addFolder(eventStore: string, namespace: string, name: string, shared: boolean): Promise<Guid> {
        const folderId = Guid.create();
        if (shared) {
            const command = new AddEventSequenceQueryFolder();
            command.eventStore = eventStore;
            command.namespace = namespace;
            command.folderId = folderId;
            command.name = name;
            await command.execute();
        } else {
            const command = new AddEventSequenceQueryFolderForUser();
            command.eventStore = eventStore;
            command.namespace = namespace;
            command.folderId = folderId;
            command.name = name;
            await command.execute();
        }
        return folderId;
    }

    addUnsavedQuery(folderId?: string) {
        const draft: QueryDefinition = {
            id: undefined,
            name: `Query ${this._queries.length + 1}`,
            eventSequenceId: 'event-log',
            folderId,
            isUnsaved: true,
        };
        this._queries.push(draft);
        this._activeTabIndex = this._queries.length - 1;
        this._currentQuery = draft;
    }

    async saveCurrentQuery(eventStore: string, namespace: string): Promise<void> {
        const query = this._currentQuery;
        if (query === undefined || query.folderId === undefined || query.isUnsaved !== true) {
            return;
        }

        const queryId = Guid.create();
        const command = new AddEventSequenceQuery();
        command.eventStore = eventStore;
        command.namespace = namespace;
        command.queryId = queryId;
        command.folderId = Guid.parse(query.folderId);
        command.name = query.name;
        command.eventSequenceId = query.eventSequenceId;

        const filter = new SequenceQueryFilter();
        filter.eventSourceId = query.eventSourceId ?? '';
        filter.eventTypes = query.eventTypes ?? [];
        filter.startTime = query.startTime ?? null as unknown as Date;
        filter.endTime = query.endTime ?? null as unknown as Date;
        command.filter = filter;

        await command.execute();

        query.id = queryId.toString();
        query.isUnsaved = false;
    }
}
