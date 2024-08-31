/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

/* eslint-disable sort-imports */
// eslint-disable-next-line header/header
import { ObservableQueryFor, QueryResultWithState, Sorting, SortingActions, SortingActionsForObservableQuery, Paging } from '@cratis/applications/queries';
import { useObservableQuery, useObservableQueryWithPaging, SetSorting, SetPage, SetPageSize } from '@cratis/applications.react/queries';
import { EventStore } from './EventStore';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/event-stores');

class AllEventStoresSortBy {
    private _name: SortingActionsForObservableQuery<EventStore[]>;
    private _description: SortingActionsForObservableQuery<EventStore[]>;

    constructor(readonly query: AllEventStores) {
        this._name = new SortingActionsForObservableQuery<EventStore[]>('name', query);
        this._description = new SortingActionsForObservableQuery<EventStore[]>('description', query);
    }

    get name(): SortingActionsForObservableQuery<EventStore[]> {
        return this._name;
    }
    get description(): SortingActionsForObservableQuery<EventStore[]> {
        return this._description;
    }
}

class AllEventStoresSortByWithoutQuery {
    private _name: SortingActions  = new SortingActions('name');
    private _description: SortingActions  = new SortingActions('description');

    get name(): SortingActions {
        return this._name;
    }
    get description(): SortingActions {
        return this._description;
    }
}

export class AllEventStores extends ObservableQueryFor<EventStore[]> {
    readonly route: string = '/api/event-stores';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: EventStore[] = [];
    private readonly _sortBy: AllEventStoresSortBy;
    private static readonly _sortBy: AllEventStoresSortByWithoutQuery = new AllEventStoresSortByWithoutQuery();

    constructor() {
        super(EventStore, true);
        this._sortBy = new AllEventStoresSortBy(this);
    }

    get requestArguments(): string[] {
        return [
        ];
    }

    get sortBy(): AllEventStoresSortBy {
        return this._sortBy;
    }

    static get sortBy(): AllEventStoresSortByWithoutQuery {
        return this._sortBy;
    }

    static use(sorting?: Sorting): [QueryResultWithState<EventStore[]>, SetSorting] {
        return useObservableQuery<EventStore[], AllEventStores>(AllEventStores, undefined, sorting);
    }

    static useWithPaging(pageSize: number, sorting?: Sorting): [QueryResultWithState<EventStore[]>, SetSorting, SetPage, SetPageSize] {
        return useObservableQueryWithPaging<EventStore[], AllEventStores>(AllEventStores, new Paging(0, pageSize), undefined, sorting);
    }
}
