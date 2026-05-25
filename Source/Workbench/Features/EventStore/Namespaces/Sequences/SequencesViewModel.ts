// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { observable } from 'mobx';
import { injectable } from 'tsyringe';
import { QueryDefinition } from './QueryDefinition';
import { CreateSequenceQuery } from 'Api/SequenceQueries';
import { SequenceQueryDefinition } from 'Api/SequenceQueries/SequenceQueryDefinition';

@injectable()
export class SequencesViewModel {

    private _queries: QueryDefinition[] = observable.array([]);
    private _currentQuery?: QueryDefinition;
    private _activeTabIndex: number = 0;

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

    setQueriesFromApi(apiQueries: SequenceQueryDefinition[]) {
        const loaded = apiQueries.map(q => ({
            id: q.id,
            name: q.name,
            eventSequenceId: 'event-log',
            eventSourceId: q.filter?.eventSourceId ?? undefined,
            eventTypes: q.filter?.eventTypes ?? [],
            startTime: q.filter?.startTime ? new Date(q.filter.startTime as unknown as string) : undefined,
            endTime: q.filter?.endTime ? new Date(q.filter.endTime as unknown as string) : undefined,
        }) as QueryDefinition);
        this._queries = observable.array(loaded);
        if (loaded.length > 0) {
            this._currentQuery = this._queries[0];
            this._activeTabIndex = 0;
        }
    }

    async addQuery(eventStore: string, namespace: string) {
        const command = new CreateSequenceQuery();
        command.eventStore = eventStore;
        command.namespace = namespace;
        command.name = `Query ${this._queries.length + 1}`;
        const result = await command.execute();
        if (result.response) {
            const newQuery: QueryDefinition = {
                id: result.response.id,
                name: result.response.name,
                eventSequenceId: 'event-log',
            };
            this._queries.push(newQuery);
            this._activeTabIndex = this._queries.length - 1;
            this._currentQuery = newQuery;
        }
    }
}
