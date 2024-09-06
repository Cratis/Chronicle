/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

/* eslint-disable sort-imports */
// eslint-disable-next-line header/header
import { QueryFor, QueryResultWithState, Sorting, SortingActions, SortingActionsForQuery, Paging } from '@cratis/applications/queries';
import { useQuery, useQueryWithPaging, PerformQuery, SetSorting, SetPage, SetPageSize } from '@cratis/applications.react/queries';
import { ObserverInformation } from '../Contracts/Observation/ObserverInformation';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/{{eventStore}}/{{namespace}}/observers');

class GetObserversSortBy {
    private _observerId: SortingActionsForQuery<ObserverInformation[]>;
    private _eventSequenceId: SortingActionsForQuery<ObserverInformation[]>;
    private _type: SortingActionsForQuery<ObserverInformation[]>;
    private _eventTypes: SortingActionsForQuery<ObserverInformation[]>;
    private _nextEventSequenceNumber: SortingActionsForQuery<ObserverInformation[]>;
    private _runningState: SortingActionsForQuery<ObserverInformation[]>;

    constructor(readonly query: GetObservers) {
        this._observerId = new SortingActionsForQuery<ObserverInformation[]>('observerId', query);
        this._eventSequenceId = new SortingActionsForQuery<ObserverInformation[]>('eventSequenceId', query);
        this._type = new SortingActionsForQuery<ObserverInformation[]>('type', query);
        this._eventTypes = new SortingActionsForQuery<ObserverInformation[]>('eventTypes', query);
        this._nextEventSequenceNumber = new SortingActionsForQuery<ObserverInformation[]>('nextEventSequenceNumber', query);
        this._runningState = new SortingActionsForQuery<ObserverInformation[]>('runningState', query);
    }

    get observerId(): SortingActionsForQuery<ObserverInformation[]> {
        return this._observerId;
    }
    get eventSequenceId(): SortingActionsForQuery<ObserverInformation[]> {
        return this._eventSequenceId;
    }
    get type(): SortingActionsForQuery<ObserverInformation[]> {
        return this._type;
    }
    get eventTypes(): SortingActionsForQuery<ObserverInformation[]> {
        return this._eventTypes;
    }
    get nextEventSequenceNumber(): SortingActionsForQuery<ObserverInformation[]> {
        return this._nextEventSequenceNumber;
    }
    get runningState(): SortingActionsForQuery<ObserverInformation[]> {
        return this._runningState;
    }
}

class GetObserversSortByWithoutQuery {
    private _observerId: SortingActions  = new SortingActions('observerId');
    private _eventSequenceId: SortingActions  = new SortingActions('eventSequenceId');
    private _type: SortingActions  = new SortingActions('type');
    private _eventTypes: SortingActions  = new SortingActions('eventTypes');
    private _nextEventSequenceNumber: SortingActions  = new SortingActions('nextEventSequenceNumber');
    private _runningState: SortingActions  = new SortingActions('runningState');

    get observerId(): SortingActions {
        return this._observerId;
    }
    get eventSequenceId(): SortingActions {
        return this._eventSequenceId;
    }
    get type(): SortingActions {
        return this._type;
    }
    get eventTypes(): SortingActions {
        return this._eventTypes;
    }
    get nextEventSequenceNumber(): SortingActions {
        return this._nextEventSequenceNumber;
    }
    get runningState(): SortingActions {
        return this._runningState;
    }
}

export interface GetObserversArguments {
    eventStore: string;
    namespace: string;
}

export class GetObservers extends QueryFor<ObserverInformation[], GetObserversArguments> {
    readonly route: string = '/api/events/store/{eventStore}/{namespace}/observers';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: ObserverInformation[] = [];
    private readonly _sortBy: GetObserversSortBy;
    private static readonly _sortBy: GetObserversSortByWithoutQuery = new GetObserversSortByWithoutQuery();

    constructor() {
        super(ObserverInformation, true);
        this._sortBy = new GetObserversSortBy(this);
    }

    get requestArguments(): string[] {
        return [
            'eventStore',
            'namespace',
        ];
    }

    get sortBy(): GetObserversSortBy {
        return this._sortBy;
    }

    static get sortBy(): GetObserversSortByWithoutQuery {
        return this._sortBy;
    }

    static use(args?: GetObserversArguments, sorting?: Sorting): [QueryResultWithState<ObserverInformation[]>, PerformQuery<GetObserversArguments>, SetSorting] {
        return useQuery<ObserverInformation[], GetObservers, GetObserversArguments>(GetObservers, args, sorting);
    }

    static useWithPaging(pageSize: number, args?: GetObserversArguments, sorting?: Sorting): [QueryResultWithState<ObserverInformation[]>, PerformQuery, SetSorting, SetPage, SetPageSize] {
        return useQueryWithPaging<ObserverInformation[], GetObservers>(GetObservers, new Paging(0, pageSize), args, sorting);
    }
}
