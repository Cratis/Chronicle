/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

/* eslint-disable sort-imports */
// eslint-disable-next-line header/header
import { QueryFor, QueryResultWithState, Sorting, SortingActions, SortingActionsForQuery, Paging } from '@cratis/applications/queries';
import { useQuery, useQueryWithPaging, PerformQuery, SetSorting, SetPage, SetPageSize } from '@cratis/applications.react/queries';
import { Identity } from './Identity';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/event-store/{{eventStore}}/{{namespace}}/identities');

class GetIdentitiesSortBy {
    private _subject: SortingActionsForQuery<Identity[]>;
    private _name: SortingActionsForQuery<Identity[]>;
    private _userName: SortingActionsForQuery<Identity[]>;
    private _onBehalfOf: SortingActionsForQuery<Identity[]>;

    constructor(readonly query: GetIdentities) {
        this._subject = new SortingActionsForQuery<Identity[]>('subject', query);
        this._name = new SortingActionsForQuery<Identity[]>('name', query);
        this._userName = new SortingActionsForQuery<Identity[]>('userName', query);
        this._onBehalfOf = new SortingActionsForQuery<Identity[]>('onBehalfOf', query);
    }

    get subject(): SortingActionsForQuery<Identity[]> {
        return this._subject;
    }
    get name(): SortingActionsForQuery<Identity[]> {
        return this._name;
    }
    get userName(): SortingActionsForQuery<Identity[]> {
        return this._userName;
    }
    get onBehalfOf(): SortingActionsForQuery<Identity[]> {
        return this._onBehalfOf;
    }
}

class GetIdentitiesSortByWithoutQuery {
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

export interface GetIdentitiesParameters {
    eventStore: string;
    namespace: string;
}

export class GetIdentities extends QueryFor<Identity[], GetIdentitiesParameters> {
    readonly route: string = '/api/event-store/{eventStore}/{namespace}/identities';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: Identity[] = [];
    private readonly _sortBy: GetIdentitiesSortBy;
    private static readonly _sortBy: GetIdentitiesSortByWithoutQuery = new GetIdentitiesSortByWithoutQuery();

    constructor() {
        super(Identity, true);
        this._sortBy = new GetIdentitiesSortBy(this);
    }

    get requiredRequestParameters(): string[] {
        return [
            'eventStore',
            'namespace',
        ];
    }

    get sortBy(): GetIdentitiesSortBy {
        return this._sortBy;
    }

    static get sortBy(): GetIdentitiesSortByWithoutQuery {
        return this._sortBy;
    }

    static use(args?: GetIdentitiesParameters, sorting?: Sorting): [QueryResultWithState<Identity[]>, PerformQuery<GetIdentitiesParameters>, SetSorting] {
        return useQuery<Identity[], GetIdentities, GetIdentitiesParameters>(GetIdentities, args, sorting);
    }

    static useWithPaging(pageSize: number, args?: GetIdentitiesParameters, sorting?: Sorting): [QueryResultWithState<Identity[]>, PerformQuery, SetSorting, SetPage, SetPageSize] {
        return useQueryWithPaging<Identity[], GetIdentities>(GetIdentities, new Paging(0, pageSize), args, sorting);
    }
}
