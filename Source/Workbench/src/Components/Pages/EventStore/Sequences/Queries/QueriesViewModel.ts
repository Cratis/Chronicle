// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


import { makeAutoObservable, } from 'mobx';

export class QueriesViewModel {

    id = 1
    queries = [{ id: 0, queryText: '', data: [] }];

    constructor() {
        makeAutoObservable(this);

    }

    addQuery() {
        const newQuery = { id: this.id++, queryText: '', data: [] };
        if (Array.isArray(this.queries)) {
            this.queries = [...this.queries, newQuery];
        }
    }

    updateQuery(id: number, newText: string) {
        this.queries = this.queries.map(query =>
            query.id === id ? { ...query, queryText: newText } : query
        );
    }

    removeQuery(id: number) {
        if (this.queries.length === 1) return;
        this.queries = this.queries.filter(query => query.id !== id);
    }
}
