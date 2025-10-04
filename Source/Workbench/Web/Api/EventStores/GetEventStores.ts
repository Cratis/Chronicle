/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

/* eslint-disable sort-imports */
// eslint-disable-next-line header/header
import { QueryFor, QueryResultWithState, Sorting, Paging } from '@cratis/applications/queries';
import { useQuery, useQueryWithPaging, PerformQuery, SetSorting, SetPage, SetPageSize } from '@cratis/applications.react/queries';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/event-stores');

class GetEventStoresSortBy {

    constructor(readonly query: GetEventStores) {
    }

}

class GetEventStoresSortByWithoutQuery {

}

export class GetEventStores extends QueryFor<string[]> {
    readonly route: string = '/api/event-stores';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: string[] = [];
    private readonly _sortBy: GetEventStoresSortBy;
    private static readonly _sortBy: GetEventStoresSortByWithoutQuery = new GetEventStoresSortByWithoutQuery();

    constructor() {
        super(String, true);
        this._sortBy = new GetEventStoresSortBy(this);
    }

    get requiredRequestParameters(): string[] {
        return [
        ];
    }

    get sortBy(): GetEventStoresSortBy {
        return this._sortBy;
    }

    static get sortBy(): GetEventStoresSortByWithoutQuery {
        return this._sortBy;
    }

    static use(sorting?: Sorting): [QueryResultWithState<string[]>, PerformQuery, SetSorting] {
        return useQuery<string[], GetEventStores>(GetEventStores, undefined, sorting);
    }

    static useWithPaging(pageSize: number, sorting?: Sorting): [QueryResultWithState<string[]>, PerformQuery, SetSorting, SetPage, SetPageSize] {
        return useQueryWithPaging<string[], GetEventStores>(GetEventStores, new Paging(0, pageSize), undefined, sorting);
    }
}
