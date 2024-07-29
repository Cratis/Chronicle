/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

// eslint-disable-next-line header/header
import { ObservableQueryFor, QueryResultWithState, Sorting, SortingActions, SortingActionsForObservableQuery, Paging } from '@cratis/applications/queries';
import { useObservableQuery, useObservableQueryWithPaging, SetSorting, SetPage, SetPageSize } from '@cratis/applications.react/queries';
import { Namespace } from './Namespace';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/namespaces');

class AllNamespacesSortBy {
    private _name: SortingActionsForObservableQuery<Namespace[]>;
    private _description: SortingActionsForObservableQuery<Namespace[]>;

    constructor(readonly query: AllNamespaces) {
        this._name = new SortingActionsForObservableQuery<Namespace[]>('name', query);
        this._description = new SortingActionsForObservableQuery<Namespace[]>('description', query);
    }

    get name(): SortingActionsForObservableQuery<Namespace[]> {
        return this._name;
    }
    get description(): SortingActionsForObservableQuery<Namespace[]> {
        return this._description;
    }
}

class AllNamespacesSortByWithoutQuery {
    private _name: SortingActions  = new SortingActions('name');
    private _description: SortingActions  = new SortingActions('description');

    get name(): SortingActions {
        return this._name;
    }
    get description(): SortingActions {
        return this._description;
    }
}

export class AllNamespaces extends ObservableQueryFor<Namespace[]> {
    readonly route: string = '/api/namespaces';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: Namespace[] = [];
    private readonly _sortBy: AllNamespacesSortBy;
    private static readonly _sortBy: AllNamespacesSortByWithoutQuery = new AllNamespacesSortByWithoutQuery();

    constructor() {
        super(Namespace, true);
        this._sortBy = new AllNamespacesSortBy(this);
    }

    get requestArguments(): string[] {
        return [
        ];
    }

    get sortBy(): AllNamespacesSortBy {
        return this._sortBy;
    }

    static get sortBy(): AllNamespacesSortByWithoutQuery {
        return this._sortBy;
    }

    static use(sorting?: Sorting): [QueryResultWithState<Namespace[]>, SetSorting] {
        return useObservableQuery<Namespace[], AllNamespaces>(AllNamespaces, undefined, sorting);
    }

    static useWithPaging(pageSize: number, sorting?: Sorting): [QueryResultWithState<Namespace[]>, SetSorting, SetPage, SetPageSize] {
        return useObservableQueryWithPaging<Namespace[], AllNamespaces>(AllNamespaces, new Paging(0, pageSize), undefined, sorting);
    }
}
