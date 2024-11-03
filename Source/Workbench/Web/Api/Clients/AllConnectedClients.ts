/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

/* eslint-disable sort-imports */
// eslint-disable-next-line header/header
import { QueryFor, QueryResultWithState, Sorting, SortingActions, SortingActionsForQuery, Paging } from '@cratis/applications/queries';
import { useQuery, useQueryWithPaging, PerformQuery, SetSorting, SetPage, SetPageSize } from '@cratis/applications.react/queries';
import { ConnectedClient } from '../Contracts/Clients/ConnectedClient';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/clients');

class AllConnectedClientsSortBy {
    private _connectionId: SortingActionsForQuery<ConnectedClient[]>;
    private _version: SortingActionsForQuery<ConnectedClient[]>;
    private _lastSeen: SortingActionsForQuery<ConnectedClient[]>;
    private _isRunningWithDebugger: SortingActionsForQuery<ConnectedClient[]>;

    constructor(readonly query: AllConnectedClients) {
        this._connectionId = new SortingActionsForQuery<ConnectedClient[]>('connectionId', query);
        this._version = new SortingActionsForQuery<ConnectedClient[]>('version', query);
        this._lastSeen = new SortingActionsForQuery<ConnectedClient[]>('lastSeen', query);
        this._isRunningWithDebugger = new SortingActionsForQuery<ConnectedClient[]>('isRunningWithDebugger', query);
    }

    get connectionId(): SortingActionsForQuery<ConnectedClient[]> {
        return this._connectionId;
    }
    get version(): SortingActionsForQuery<ConnectedClient[]> {
        return this._version;
    }
    get lastSeen(): SortingActionsForQuery<ConnectedClient[]> {
        return this._lastSeen;
    }
    get isRunningWithDebugger(): SortingActionsForQuery<ConnectedClient[]> {
        return this._isRunningWithDebugger;
    }
}

class AllConnectedClientsSortByWithoutQuery {
    private _connectionId: SortingActions  = new SortingActions('connectionId');
    private _version: SortingActions  = new SortingActions('version');
    private _lastSeen: SortingActions  = new SortingActions('lastSeen');
    private _isRunningWithDebugger: SortingActions  = new SortingActions('isRunningWithDebugger');

    get connectionId(): SortingActions {
        return this._connectionId;
    }
    get version(): SortingActions {
        return this._version;
    }
    get lastSeen(): SortingActions {
        return this._lastSeen;
    }
    get isRunningWithDebugger(): SortingActions {
        return this._isRunningWithDebugger;
    }
}

export class AllConnectedClients extends QueryFor<ConnectedClient[]> {
    readonly route: string = '/api/clients';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: ConnectedClient[] = [];
    private readonly _sortBy: AllConnectedClientsSortBy;
    private static readonly _sortBy: AllConnectedClientsSortByWithoutQuery = new AllConnectedClientsSortByWithoutQuery();

    constructor() {
        super(ConnectedClient, true);
        this._sortBy = new AllConnectedClientsSortBy(this);
    }

    get requiredRequestArguments(): string[] {
        return [
        ];
    }

    get sortBy(): AllConnectedClientsSortBy {
        return this._sortBy;
    }

    static get sortBy(): AllConnectedClientsSortByWithoutQuery {
        return this._sortBy;
    }

    static use(sorting?: Sorting): [QueryResultWithState<ConnectedClient[]>, PerformQuery, SetSorting] {
        return useQuery<ConnectedClient[], AllConnectedClients>(AllConnectedClients, undefined, sorting);
    }

    static useWithPaging(pageSize: number, sorting?: Sorting): [QueryResultWithState<ConnectedClient[]>, PerformQuery, SetSorting, SetPage, SetPageSize] {
        return useQueryWithPaging<ConnectedClient[], AllConnectedClients>(AllConnectedClients, new Paging(0, pageSize), undefined, sorting);
    }
}
