/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

// eslint-disable-next-line header/header
import { QueryFor, QueryResultWithState, Sorting, SortingActions, SortingActionsForQuery, Paging } from '@cratis/applications/queries';
import { useQuery, useQueryWithPaging, PerformQuery, SetSorting, SetPage, SetPageSize } from '@cratis/applications.react/queries';
import { ObserverInformation } from '../Chronicle/Contracts/Observation/ObserverInformation';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/{eventStore}/{namespace}/observers/observe');

class AllObserversSortBy {
    private _observerId: SortingActionsForQuery<ObserverInformation[]>;
    private _eventSequenceId: SortingActionsForQuery<ObserverInformation[]>;
    private _type: SortingActionsForQuery<ObserverInformation[]>;
    private _eventTypes: SortingActionsForQuery<ObserverInformation[]>;
    private _nextEventSequenceNumber: SortingActionsForQuery<ObserverInformation[]>;
    private _runningState: SortingActionsForQuery<ObserverInformation[]>;

    constructor(readonly query: AllObservers) {
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

class AllObserversSortByWithoutQuery {
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

export interface AllObserversArguments {
    eventStore: string;
    namespace: string;
}

export class AllObservers extends QueryFor<ObserverInformation[], AllObserversArguments> {
    readonly route: string = '/api/events/store/{eventStore}/{namespace}/observers/observe';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: ObserverInformation[] = [];
    private readonly _sortBy: AllObserversSortBy;
    private static readonly _sortBy: AllObserversSortByWithoutQuery = new AllObserversSortByWithoutQuery();

    constructor() {
        super(ObserverInformation, true);
        this._sortBy = new AllObserversSortBy(this);
    }

    get requestArguments(): string[] {
        return [
            'eventStore',
            'namespace',
        ];
    }

    get sortBy(): AllObserversSortBy {
        return this._sortBy;
    }

    static get sortBy(): AllObserversSortByWithoutQuery {
        return this._sortBy;
    }

    static use(args?: AllObserversArguments, sorting?: Sorting): [QueryResultWithState<ObserverInformation[]>, PerformQuery<AllObserversArguments>] {
        return useQuery<ObserverInformation[], AllObservers, AllObserversArguments>(AllObservers, args, sorting);
    }

    static useWithPaging(pageSize: number, args?: AllObserversArguments, sorting?: Sorting): [QueryResultWithState<ObserverInformation[]>, number, PerformQuery, SetSorting, SetPage, SetPageSize] {
        return useQueryWithPaging<ObserverInformation[], AllObservers>(AllObservers, new Paging(0, pageSize), args, sorting);
    }
}
