/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

/* eslint-disable sort-imports */
// eslint-disable-next-line header/header
import { ObservableQueryFor, QueryResultWithState, Sorting, SortingActions, SortingActionsForObservableQuery, Paging } from '@cratis/applications/queries';
import { useObservableQuery, useObservableQueryWithPaging, SetSorting, SetPage, SetPageSize } from '@cratis/applications.react/queries';
import { ObserverInformation } from '../Concepts/Observation/ObserverInformation';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/event-store/{{eventStore}}/{{namespace}}/observers/all-observers/observe');

class AllObserversSortBy {
    private _observerId: SortingActionsForObservableQuery<ObserverInformation[]>;
    private _eventSequenceId: SortingActionsForObservableQuery<ObserverInformation[]>;
    private _type: SortingActionsForObservableQuery<ObserverInformation[]>;
    private _eventTypes: SortingActionsForObservableQuery<ObserverInformation[]>;
    private _nextEventSequenceNumber: SortingActionsForObservableQuery<ObserverInformation[]>;
    private _lastHandledEventSequenceNumber: SortingActionsForObservableQuery<ObserverInformation[]>;
    private _runningState: SortingActionsForObservableQuery<ObserverInformation[]>;
    private _failedPartitions: SortingActionsForObservableQuery<ObserverInformation[]>;

    constructor(readonly query: AllObservers) {
        this._observerId = new SortingActionsForObservableQuery<ObserverInformation[]>('observerId', query);
        this._eventSequenceId = new SortingActionsForObservableQuery<ObserverInformation[]>('eventSequenceId', query);
        this._type = new SortingActionsForObservableQuery<ObserverInformation[]>('type', query);
        this._eventTypes = new SortingActionsForObservableQuery<ObserverInformation[]>('eventTypes', query);
        this._nextEventSequenceNumber = new SortingActionsForObservableQuery<ObserverInformation[]>('nextEventSequenceNumber', query);
        this._lastHandledEventSequenceNumber = new SortingActionsForObservableQuery<ObserverInformation[]>('lastHandledEventSequenceNumber', query);
        this._runningState = new SortingActionsForObservableQuery<ObserverInformation[]>('runningState', query);
        this._failedPartitions = new SortingActionsForObservableQuery<ObserverInformation[]>('failedPartitions', query);
    }

    get observerId(): SortingActionsForObservableQuery<ObserverInformation[]> {
        return this._observerId;
    }
    get eventSequenceId(): SortingActionsForObservableQuery<ObserverInformation[]> {
        return this._eventSequenceId;
    }
    get type(): SortingActionsForObservableQuery<ObserverInformation[]> {
        return this._type;
    }
    get eventTypes(): SortingActionsForObservableQuery<ObserverInformation[]> {
        return this._eventTypes;
    }
    get nextEventSequenceNumber(): SortingActionsForObservableQuery<ObserverInformation[]> {
        return this._nextEventSequenceNumber;
    }
    get lastHandledEventSequenceNumber(): SortingActionsForObservableQuery<ObserverInformation[]> {
        return this._lastHandledEventSequenceNumber;
    }
    get runningState(): SortingActionsForObservableQuery<ObserverInformation[]> {
        return this._runningState;
    }
    get failedPartitions(): SortingActionsForObservableQuery<ObserverInformation[]> {
        return this._failedPartitions;
    }
}

class AllObserversSortByWithoutQuery {
    private _observerId: SortingActions  = new SortingActions('observerId');
    private _eventSequenceId: SortingActions  = new SortingActions('eventSequenceId');
    private _type: SortingActions  = new SortingActions('type');
    private _eventTypes: SortingActions  = new SortingActions('eventTypes');
    private _nextEventSequenceNumber: SortingActions  = new SortingActions('nextEventSequenceNumber');
    private _lastHandledEventSequenceNumber: SortingActions  = new SortingActions('lastHandledEventSequenceNumber');
    private _runningState: SortingActions  = new SortingActions('runningState');
    private _failedPartitions: SortingActions  = new SortingActions('failedPartitions');

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
    get lastHandledEventSequenceNumber(): SortingActions {
        return this._lastHandledEventSequenceNumber;
    }
    get runningState(): SortingActions {
        return this._runningState;
    }
    get failedPartitions(): SortingActions {
        return this._failedPartitions;
    }
}

export interface AllObserversArguments {
    eventStore: string;
    namespace: string;
}
export class AllObservers extends ObservableQueryFor<ObserverInformation[], AllObserversArguments> {
    readonly route: string = '/api/event-store/{eventStore}/{namespace}/observers/all-observers/observe';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: ObserverInformation[] = [];
    private readonly _sortBy: AllObserversSortBy;
    private static readonly _sortBy: AllObserversSortByWithoutQuery = new AllObserversSortByWithoutQuery();

    constructor() {
        super(ObserverInformation, true);
        this._sortBy = new AllObserversSortBy(this);
    }

    get requiredRequestArguments(): string[] {
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

    static use(args?: AllObserversArguments, sorting?: Sorting): [QueryResultWithState<ObserverInformation[]>, SetSorting] {
        return useObservableQuery<ObserverInformation[], AllObservers, AllObserversArguments>(AllObservers, args, sorting);
    }

    static useWithPaging(pageSize: number, args?: AllObserversArguments, sorting?: Sorting): [QueryResultWithState<ObserverInformation[]>, SetSorting, SetPage, SetPageSize] {
        return useObservableQueryWithPaging<ObserverInformation[], AllObservers>(AllObservers, new Paging(0, pageSize), args, sorting);
    }
}
