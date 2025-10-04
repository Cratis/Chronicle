/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

/* eslint-disable sort-imports */
// eslint-disable-next-line header/header
import { ObservableQueryFor, QueryResultWithState, Sorting, SortingActions, SortingActionsForObservableQuery, Paging } from '@cratis/applications/queries';
import { useObservableQuery, useObservableQueryWithPaging, SetSorting, SetPage, SetPageSize } from '@cratis/applications.react/queries';
import { Identity } from './Identity';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/event-store/{{eventStore}}/{{namespace}}/identities/observe');

class AllIdentitiesSortBy {
    private _subject: SortingActionsForObservableQuery<Identity[]>;
    private _name: SortingActionsForObservableQuery<Identity[]>;
    private _userName: SortingActionsForObservableQuery<Identity[]>;
    private _onBehalfOf: SortingActionsForObservableQuery<Identity[]>;

    constructor(readonly query: AllIdentities) {
        this._subject = new SortingActionsForObservableQuery<Identity[]>('subject', query);
        this._name = new SortingActionsForObservableQuery<Identity[]>('name', query);
        this._userName = new SortingActionsForObservableQuery<Identity[]>('userName', query);
        this._onBehalfOf = new SortingActionsForObservableQuery<Identity[]>('onBehalfOf', query);
    }

    get subject(): SortingActionsForObservableQuery<Identity[]> {
        return this._subject;
    }
    get name(): SortingActionsForObservableQuery<Identity[]> {
        return this._name;
    }
    get userName(): SortingActionsForObservableQuery<Identity[]> {
        return this._userName;
    }
    get onBehalfOf(): SortingActionsForObservableQuery<Identity[]> {
        return this._onBehalfOf;
    }
}

class AllIdentitiesSortByWithoutQuery {
    private _subject: SortingActions  = new SortingActions('subject');
    private _name: SortingActions  = new SortingActions('name');
    private _userName: SortingActions  = new SortingActions('userName');
    private _onBehalfOf: SortingActions  = new SortingActions('onBehalfOf');

    get subject(): SortingActions {
        return this._subject;
    }
    get name(): SortingActions {
        return this._name;
    }
    get userName(): SortingActions {
        return this._userName;
    }
    get onBehalfOf(): SortingActions {
        return this._onBehalfOf;
    }
}

export interface AllIdentitiesParameters {
    eventStore: string;
    namespace: string;
}
export class AllIdentities extends ObservableQueryFor<Identity[], AllIdentitiesParameters> {
    readonly route: string = '/api/event-store/{eventStore}/{namespace}/identities/observe';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: Identity[] = [];
    private readonly _sortBy: AllIdentitiesSortBy;
    private static readonly _sortBy: AllIdentitiesSortByWithoutQuery = new AllIdentitiesSortByWithoutQuery();

    constructor() {
        super(Identity, true);
        this._sortBy = new AllIdentitiesSortBy(this);
    }

    get requiredRequestParameters(): string[] {
        return [
            'eventStore',
            'namespace',
        ];
    }

    get sortBy(): AllIdentitiesSortBy {
        return this._sortBy;
    }

    static get sortBy(): AllIdentitiesSortByWithoutQuery {
        return this._sortBy;
    }

    static use(args?: AllIdentitiesParameters, sorting?: Sorting): [QueryResultWithState<Identity[]>, SetSorting] {
        return useObservableQuery<Identity[], AllIdentities, AllIdentitiesParameters>(AllIdentities, args, sorting);
    }

    static useWithPaging(pageSize: number, args?: AllIdentitiesParameters, sorting?: Sorting): [QueryResultWithState<Identity[]>, SetSorting, SetPage, SetPageSize] {
        return useObservableQueryWithPaging<Identity[], AllIdentities>(AllIdentities, new Paging(0, pageSize), args, sorting);
    }
}
