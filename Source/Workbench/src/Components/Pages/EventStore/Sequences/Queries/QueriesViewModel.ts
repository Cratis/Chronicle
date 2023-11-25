// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export class QueriesViewModel {

    queries = [
        { title: 'Query 1', id: '1' },
        { title: 'Query 2', id: '2' },
        { title: 'Query 3', id: '3' }
    ];

    currentQuery = 0;

    setCurrentQuery(idx: number) {
        this.currentQuery = idx;
    }

    addQuery() {
        const newQueryIdx = this.queries.length + 1;
        const newQuery = {
            title: `Query ${newQueryIdx}`,
            id: ` ${newQueryIdx} Content id`
        };
        this.queries.push(newQuery);
    }

    panelClassName(idx: number) {
        return this.currentQuery === idx ? 'bg-primary' : '';
    }
}
