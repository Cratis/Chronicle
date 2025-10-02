/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

/* eslint-disable sort-imports */
// eslint-disable-next-line header/header
import { ObservableQueryFor, QueryResultWithState, Sorting, Paging } from '@cratis/applications/queries';
import { useObservableQuery, useObservableQueryWithPaging, SetSorting, SetPage, SetPageSize } from '@cratis/applications.react/queries';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/event-store/{{eventStore}}/namespaces');

class AllNamespacesSortBy {

    constructor(readonly query: AllNamespaces) {
    }

}

class AllNamespacesSortByWithoutQuery {

}

export interface AllNamespacesParameters {
    eventStore: string;
}
export class AllNamespaces extends ObservableQueryFor<string[], AllNamespacesParameters> {
    readonly route: string = '/api/event-store/{eventStore}/namespaces';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: string[] = [];
    private readonly _sortBy: AllNamespacesSortBy;
    private static readonly _sortBy: AllNamespacesSortByWithoutQuery = new AllNamespacesSortByWithoutQuery();

    constructor() {
        super(String, true);
        this._sortBy = new AllNamespacesSortBy(this);
    }

    get requiredRequestParameters(): string[] {
        return [
            'eventStore',
        ];
    }

    get sortBy(): AllNamespacesSortBy {
        return this._sortBy;
    }

    static get sortBy(): AllNamespacesSortByWithoutQuery {
        return this._sortBy;
    }

    static use(args?: AllNamespacesParameters, sorting?: Sorting): [QueryResultWithState<string[]>, SetSorting] {
        return useObservableQuery<string[], AllNamespaces, AllNamespacesParameters>(AllNamespaces, args, sorting);
    }

    static useWithPaging(pageSize: number, args?: AllNamespacesParameters, sorting?: Sorting): [QueryResultWithState<string[]>, SetSorting, SetPage, SetPageSize] {
        return useObservableQueryWithPaging<string[], AllNamespaces>(AllNamespaces, new Paging(0, pageSize), args, sorting);
    }
}
