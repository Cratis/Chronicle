/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

/* eslint-disable sort-imports */
// eslint-disable-next-line header/header
import { ObservableQueryFor, QueryResultWithState, Sorting, SortingActions, SortingActionsForObservableQuery, Paging } from '@cratis/applications/queries';
import { useObservableQuery, useObservableQueryWithPaging, SetSorting, SetPage, SetPageSize } from '@cratis/applications.react/queries';
import { ObserverInformation } from '../Contracts/Observation/ObserverInformation';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/{{eventStore}}/{{namespace}}/observers/observe');

class AllObserversSortBy {
    private _observerId: SortingActionsForObservableQuery<ObserverInformation[]>;
    private _eventSequenceId: SortingActionsForObservableQuery<ObserverInformation[]>;
    private _type: SortingActionsForObservableQuery<ObserverInformation[]>;
    private _eventTypes: SortingActionsForObservableQuery<ObserverInformation[]>;
    private _nextEventSequenceNumber: SortingActionsForObservableQuery<ObserverInformation[]>;
    private _runningState: SortingActionsForObservableQuery<ObserverInformation[]>;

    constructor(readonly query: AllObservers) {
        this._observerId = new SortingActionsForObservableQuery<ObserverInformation[]>('observerId', query);
        this._eventSequenceId = new SortingActionsForObservableQuery<ObserverInformation[]>('eventSequenceId', query);
        this._type = new SortingActionsForObservableQuery<ObserverInformation[]>('type', query);
        this._eventTypes = new SortingActionsForObservableQuery<ObserverInformation[]>('eventTypes', query);
        this._nextEventSequenceNumber = new SortingActionsForObservableQuery<ObserverInformation[]>('nextEventSequenceNumber', query);
        this._runningState = new SortingActionsForObservableQuery<ObserverInformation[]>('runningState', query);
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
    get runningState(): SortingActionsForObservableQuery<ObserverInformation[]> {
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
export class AllObservers extends ObservableQueryFor<ObserverInformation[], AllObserversArguments> {
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

    static use(args?: AllObserversArguments, sorting?: Sorting): [QueryResultWithState<ObserverInformation[]>, SetSorting] {
        return useObservableQuery<ObserverInformation[], AllObservers, AllObserversArguments>(AllObservers, args, sorting);
    }

    static useWithPaging(pageSize: number, args?: AllObserversArguments, sorting?: Sorting): [QueryResultWithState<ObserverInformation[]>, SetSorting, SetPage, SetPageSize] {
        return useObservableQueryWithPaging<ObserverInformation[], AllObservers>(AllObservers, new Paging(0, pageSize), args, sorting);
    }
}
