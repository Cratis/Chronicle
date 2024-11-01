/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

/* eslint-disable sort-imports */
// eslint-disable-next-line header/header
import { QueryFor, QueryResultWithState, Sorting, SortingActions, SortingActionsForQuery, Paging } from '@cratis/applications/queries';
import { useQuery, useQueryWithPaging, PerformQuery, SetSorting, SetPage, SetPageSize } from '@cratis/applications.react/queries';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/event-stores');

class GetEventStoresSortBy {
    private _value: SortingActionsForQuery<string[]>;

    constructor(readonly query: GetEventStores) {
        this._value = new SortingActionsForQuery<string[]>('value', query);
    }

    get value(): SortingActionsForQuery<string[]> {
        return this._value;
    }
}

class GetEventStoresSortByWithoutQuery {
    private _value: SortingActions  = new SortingActions('value');

    get value(): SortingActions {
        return this._value;
    }
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

    get requiredRequestArguments(): string[] {
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
