/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

/* eslint-disable sort-imports */
// eslint-disable-next-line header/header
import { ObservableQueryFor, QueryResultWithState, Sorting, SortingActions, SortingActionsForObservableQuery, Paging } from '@cratis/applications/queries';
import { useObservableQuery, useObservableQueryWithPaging, SetSorting, SetPage, SetPageSize } from '@cratis/applications.react/queries';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/event-stores/observe');

class AllEventStoresSortBy {

    constructor(readonly query: AllEventStores) {
    }

}

class AllEventStoresSortByWithoutQuery {

}

export class AllEventStores extends ObservableQueryFor<string[]> {
    readonly route: string = '/api/event-stores/observe';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: string[] = [];
    private readonly _sortBy: AllEventStoresSortBy;
    private static readonly _sortBy: AllEventStoresSortByWithoutQuery = new AllEventStoresSortByWithoutQuery();

    constructor() {
        super(String, true);
        this._sortBy = new AllEventStoresSortBy(this);
    }

    get requiredRequestArguments(): string[] {
        return [
        ];
    }

    get sortBy(): AllEventStoresSortBy {
        return this._sortBy;
    }

    static get sortBy(): AllEventStoresSortByWithoutQuery {
        return this._sortBy;
    }

    static use(sorting?: Sorting): [QueryResultWithState<string[]>, SetSorting] {
        return useObservableQuery<string[], AllEventStores>(AllEventStores, undefined, sorting);
    }

    static useWithPaging(pageSize: number, sorting?: Sorting): [QueryResultWithState<string[]>, SetSorting, SetPage, SetPageSize] {
        return useObservableQueryWithPaging<string[], AllEventStores>(AllEventStores, new Paging(0, pageSize), undefined, sorting);
    }
}
