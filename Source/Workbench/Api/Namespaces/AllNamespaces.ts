/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

/* eslint-disable sort-imports */
// eslint-disable-next-line header/header
import { ObservableQueryFor, QueryResultWithState, Sorting, SortingActions, SortingActionsForObservableQuery, Paging } from '@cratis/applications/queries';
import { useObservableQuery, useObservableQueryWithPaging, SetSorting, SetPage, SetPageSize } from '@cratis/applications.react/queries';
import { Namespace } from './Namespace';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/event-store/{{eventStore}}/namespaces');

class AllNamespacesSortBy {
    private _id: SortingActionsForObservableQuery<Namespace[]>;
    private _name: SortingActionsForObservableQuery<Namespace[]>;
    private _description: SortingActionsForObservableQuery<Namespace[]>;

    constructor(readonly query: AllNamespaces) {
        this._id = new SortingActionsForObservableQuery<Namespace[]>('id', query);
        this._name = new SortingActionsForObservableQuery<Namespace[]>('name', query);
        this._description = new SortingActionsForObservableQuery<Namespace[]>('description', query);
    }

    get id(): SortingActionsForObservableQuery<Namespace[]> {
        return this._id;
    }
    get name(): SortingActionsForObservableQuery<Namespace[]> {
        return this._name;
    }
    get description(): SortingActionsForObservableQuery<Namespace[]> {
        return this._description;
    }
}

class AllNamespacesSortByWithoutQuery {
    private _id: SortingActions  = new SortingActions('id');
    private _name: SortingActions  = new SortingActions('name');
    private _description: SortingActions  = new SortingActions('description');

    get id(): SortingActions {
        return this._id;
    }
    get name(): SortingActions {
        return this._name;
    }
    get description(): SortingActions {
        return this._description;
    }
}

export interface AllNamespacesArguments {
    eventStore: string;
}
export class AllNamespaces extends ObservableQueryFor<Namespace[], AllNamespacesArguments> {
    readonly route: string = '/api/event-store/{eventStore}/namespaces';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: Namespace[] = [];
    private readonly _sortBy: AllNamespacesSortBy;
    private static readonly _sortBy: AllNamespacesSortByWithoutQuery = new AllNamespacesSortByWithoutQuery();

    constructor() {
        super(Namespace, true);
        this._sortBy = new AllNamespacesSortBy(this);
    }

    get requiredRequestArguments(): string[] {
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

    static use(args?: AllNamespacesArguments, sorting?: Sorting): [QueryResultWithState<Namespace[]>, SetSorting] {
        return useObservableQuery<Namespace[], AllNamespaces, AllNamespacesArguments>(AllNamespaces, args, sorting);
    }

    static useWithPaging(pageSize: number, args?: AllNamespacesArguments, sorting?: Sorting): [QueryResultWithState<Namespace[]>, SetSorting, SetPage, SetPageSize] {
        return useObservableQueryWithPaging<Namespace[], AllNamespaces>(AllNamespaces, new Paging(0, pageSize), args, sorting);
    }
}
