/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

/* eslint-disable sort-imports */
// eslint-disable-next-line header/header
import { ObservableQueryFor, QueryResultWithState, Sorting, SortingActions, SortingActionsForObservableQuery, Paging } from '@cratis/arc/queries';
import { useObservableQuery, useObservableQueryWithPaging, SetSorting, SetPage, SetPageSize } from '@cratis/arc.react/queries';
import { ParameterDescriptor } from '@cratis/arc/reflection';
import { Job } from './Job';

class AllJobsSortBy {
    private _id: SortingActionsForObservableQuery<Job[]>;
    private _details: SortingActionsForObservableQuery<Job[]>;
    private _type: SortingActionsForObservableQuery<Job[]>;
    private _status: SortingActionsForObservableQuery<Job[]>;
    private _created: SortingActionsForObservableQuery<Job[]>;
    private _statusChanges: SortingActionsForObservableQuery<Job[]>;
    private _progress: SortingActionsForObservableQuery<Job[]>;

    constructor(readonly query: AllJobs) {
        this._id = new SortingActionsForObservableQuery<Job[]>('id', query);
        this._details = new SortingActionsForObservableQuery<Job[]>('details', query);
        this._type = new SortingActionsForObservableQuery<Job[]>('type', query);
        this._status = new SortingActionsForObservableQuery<Job[]>('status', query);
        this._created = new SortingActionsForObservableQuery<Job[]>('created', query);
        this._statusChanges = new SortingActionsForObservableQuery<Job[]>('statusChanges', query);
        this._progress = new SortingActionsForObservableQuery<Job[]>('progress', query);
    }

    get id(): SortingActionsForObservableQuery<Job[]> {
        return this._id;
    }
    get details(): SortingActionsForObservableQuery<Job[]> {
        return this._details;
    }
    get type(): SortingActionsForObservableQuery<Job[]> {
        return this._type;
    }
    get status(): SortingActionsForObservableQuery<Job[]> {
        return this._status;
    }
    get created(): SortingActionsForObservableQuery<Job[]> {
        return this._created;
    }
    get statusChanges(): SortingActionsForObservableQuery<Job[]> {
        return this._statusChanges;
    }
    get progress(): SortingActionsForObservableQuery<Job[]> {
        return this._progress;
    }
}

class AllJobsSortByWithoutQuery {
    private _id: SortingActions  = new SortingActions('id');
    private _details: SortingActions  = new SortingActions('details');
    private _type: SortingActions  = new SortingActions('type');
    private _status: SortingActions  = new SortingActions('status');
    private _created: SortingActions  = new SortingActions('created');
    private _statusChanges: SortingActions  = new SortingActions('statusChanges');
    private _progress: SortingActions  = new SortingActions('progress');

    get id(): SortingActions {
        return this._id;
    }
    get details(): SortingActions {
        return this._details;
    }
    get type(): SortingActions {
        return this._type;
    }
    get status(): SortingActions {
        return this._status;
    }
    get created(): SortingActions {
        return this._created;
    }
    get statusChanges(): SortingActions {
        return this._statusChanges;
    }
    get progress(): SortingActions {
        return this._progress;
    }
}

export interface AllJobsParameters {
    eventStore: string;
    namespace: string;
}
export class AllJobs extends ObservableQueryFor<Job[], AllJobsParameters> {
    readonly route: string = '/api/event-store/{eventStore}/{namespace}/jobs';
    readonly defaultValue: Job[] = [];
    private readonly _sortBy: AllJobsSortBy;
    private static readonly _sortBy: AllJobsSortByWithoutQuery = new AllJobsSortByWithoutQuery();

    constructor() {
        super(Job, true);
        this._sortBy = new AllJobsSortBy(this);
    }

    get requiredRequestParameters(): string[] {
        return [
            'eventStore',
            'namespace',
        ];
    }

    readonly parameterDescriptors: ParameterDescriptor[] = [
        new ParameterDescriptor('eventStore', String),
        new ParameterDescriptor('namespace', String),
    ];

    eventStore!: string;
    namespace!: string;

    get sortBy(): AllJobsSortBy {
        return this._sortBy;
    }

    static get sortBy(): AllJobsSortByWithoutQuery {
        return this._sortBy;
    }

    static use(args?: AllJobsParameters, sorting?: Sorting): [QueryResultWithState<Job[]>, SetSorting] {
        return useObservableQuery<Job[], AllJobs, AllJobsParameters>(AllJobs, args, sorting);
    }

    static useWithPaging(pageSize: number, args?: AllJobsParameters, sorting?: Sorting): [QueryResultWithState<Job[]>, SetSorting, SetPage, SetPageSize] {
        return useObservableQueryWithPaging<Job[], AllJobs>(AllJobs, new Paging(0, pageSize), args, sorting);
    }
}
