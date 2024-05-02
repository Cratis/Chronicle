// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { observable } from 'mobx';
import { injectable } from 'tsyringe';
import { QueryDefinition } from './QueryDefinition';

@injectable()
export class SequencesViewModel {

    private _queries: QueryDefinition[] = [];
    private _currentQuery?: QueryDefinition;

    constructor() {
        this._queries = observable.array([{
            name: 'Query 1'
        }] as QueryDefinition[]);

        this._currentQuery = this._queries[0];
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

    addQuery() {
        this._queries.push({ name: 'New Query' });
    }
}
