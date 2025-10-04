/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

/* eslint-disable sort-imports */
// eslint-disable-next-line header/header
import { ObservableQueryFor, QueryResultWithState, Sorting, SortingActions, SortingActionsForObservableQuery, Paging } from '@cratis/applications/queries';
import { useObservableQuery, useObservableQueryWithPaging, SetSorting, SetPage, SetPageSize } from '@cratis/applications.react/queries';
import { ObserverInformation } from './ObserverInformation';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/event-store/{{eventStore}}/{{namespace}}/observers/all-observers/observe');

class AllObserversSortBy {
    private _id: SortingActionsForObservableQuery<ObserverInformation[]>;
    private _eventSequenceId: SortingActionsForObservableQuery<ObserverInformation[]>;
    private _type: SortingActionsForObservableQuery<ObserverInformation[]>;
    private _owner: SortingActionsForObservableQuery<ObserverInformation[]>;
    private _eventTypes: SortingActionsForObservableQuery<ObserverInformation[]>;
    private _nextEventSequenceNumber: SortingActionsForObservableQuery<ObserverInformation[]>;
    private _lastHandledEventSequenceNumber: SortingActionsForObservableQuery<ObserverInformation[]>;
    private _runningState: SortingActionsForObservableQuery<ObserverInformation[]>;
    private _isSubscribed: SortingActionsForObservableQuery<ObserverInformation[]>;

    constructor(readonly query: AllObservers) {
        this._id = new SortingActionsForObservableQuery<ObserverInformation[]>('id', query);
        this._eventSequenceId = new SortingActionsForObservableQuery<ObserverInformation[]>('eventSequenceId', query);
        this._type = new SortingActionsForObservableQuery<ObserverInformation[]>('type', query);
        this._owner = new SortingActionsForObservableQuery<ObserverInformation[]>('owner', query);
        this._eventTypes = new SortingActionsForObservableQuery<ObserverInformation[]>('eventTypes', query);
        this._nextEventSequenceNumber = new SortingActionsForObservableQuery<ObserverInformation[]>('nextEventSequenceNumber', query);
        this._lastHandledEventSequenceNumber = new SortingActionsForObservableQuery<ObserverInformation[]>('lastHandledEventSequenceNumber', query);
        this._runningState = new SortingActionsForObservableQuery<ObserverInformation[]>('runningState', query);
        this._isSubscribed = new SortingActionsForObservableQuery<ObserverInformation[]>('isSubscribed', query);
    }

    get id(): SortingActionsForObservableQuery<ObserverInformation[]> {
        return this._id;
    }
    get eventSequenceId(): SortingActionsForObservableQuery<ObserverInformation[]> {
        return this._eventSequenceId;
    }
    get type(): SortingActionsForObservableQuery<ObserverInformation[]> {
        return this._type;
    }
    get owner(): SortingActionsForObservableQuery<ObserverInformation[]> {
        return this._owner;
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
    get isSubscribed(): SortingActionsForObservableQuery<ObserverInformation[]> {
        return this._isSubscribed;
    }
}

class AllObserversSortByWithoutQuery {
    private _id: SortingActions  = new SortingActions('id');
    private _eventSequenceId: SortingActions  = new SortingActions('eventSequenceId');
    private _type: SortingActions  = new SortingActions('type');
    private _owner: SortingActions  = new SortingActions('owner');
    private _eventTypes: SortingActions  = new SortingActions('eventTypes');
    private _nextEventSequenceNumber: SortingActions  = new SortingActions('nextEventSequenceNumber');
    private _lastHandledEventSequenceNumber: SortingActions  = new SortingActions('lastHandledEventSequenceNumber');
    private _runningState: SortingActions  = new SortingActions('runningState');
    private _isSubscribed: SortingActions  = new SortingActions('isSubscribed');

    get id(): SortingActions {
        return this._id;
    }
    get eventSequenceId(): SortingActions {
        return this._eventSequenceId;
    }
    get type(): SortingActions {
        return this._type;
    }
    get owner(): SortingActions {
        return this._owner;
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
    get isSubscribed(): SortingActions {
        return this._isSubscribed;
    }
}

export interface AllObserversParameters {
    eventStore: string;
    namespace: string;
}
export class AllObservers extends ObservableQueryFor<ObserverInformation[], AllObserversParameters> {
    readonly route: string = '/api/event-store/{eventStore}/{namespace}/observers/all-observers/observe';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: ObserverInformation[] = [];
    private readonly _sortBy: AllObserversSortBy;
    private static readonly _sortBy: AllObserversSortByWithoutQuery = new AllObserversSortByWithoutQuery();

    constructor() {
        super(ObserverInformation, true);
        this._sortBy = new AllObserversSortBy(this);
    }

    get requiredRequestParameters(): string[] {
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

    static use(args?: AllObserversParameters, sorting?: Sorting): [QueryResultWithState<ObserverInformation[]>, SetSorting] {
        return useObservableQuery<ObserverInformation[], AllObservers, AllObserversParameters>(AllObservers, args, sorting);
    }

    static useWithPaging(pageSize: number, args?: AllObserversParameters, sorting?: Sorting): [QueryResultWithState<ObserverInformation[]>, SetSorting, SetPage, SetPageSize] {
        return useObservableQueryWithPaging<ObserverInformation[], AllObservers>(AllObservers, new Paging(0, pageSize), args, sorting);
    }
}
