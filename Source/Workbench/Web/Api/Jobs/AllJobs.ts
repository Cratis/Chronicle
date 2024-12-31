/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

/* eslint-disable sort-imports */
// eslint-disable-next-line header/header
import { ObservableQueryFor, QueryResultWithState, Sorting, SortingActions, SortingActionsForObservableQuery, Paging } from '@cratis/applications/queries';
import { useObservableQuery, useObservableQueryWithPaging, SetSorting, SetPage, SetPageSize } from '@cratis/applications.react/queries';
import { Job } from '../Contracts/Jobs/Job';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/event-store/{{eventStore}}/{{namespace}}/jobs');

class AllJobsSortBy {
    private _id: SortingActionsForObservableQuery<Job[]>;
    private _name: SortingActionsForObservableQuery<Job[]>;
    private _details: SortingActionsForObservableQuery<Job[]>;
    private _type: SortingActionsForObservableQuery<Job[]>;
    private _status: SortingActionsForObservableQuery<Job[]>;
    private _statusChanges: SortingActionsForObservableQuery<Job[]>;
    private _progress: SortingActionsForObservableQuery<Job[]>;

    constructor(readonly query: AllJobs) {
        this._id = new SortingActionsForObservableQuery<Job[]>('id', query);
        this._name = new SortingActionsForObservableQuery<Job[]>('name', query);
        this._details = new SortingActionsForObservableQuery<Job[]>('details', query);
        this._type = new SortingActionsForObservableQuery<Job[]>('type', query);
        this._status = new SortingActionsForObservableQuery<Job[]>('status', query);
        this._statusChanges = new SortingActionsForObservableQuery<Job[]>('statusChanges', query);
        this._progress = new SortingActionsForObservableQuery<Job[]>('progress', query);
    }

    get id(): SortingActionsForObservableQuery<Job[]> {
        return this._id;
    }
    get name(): SortingActionsForObservableQuery<Job[]> {
        return this._name;
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
    get statusChanges(): SortingActionsForObservableQuery<Job[]> {
        return this._statusChanges;
    }
    get progress(): SortingActionsForObservableQuery<Job[]> {
        return this._progress;
    }
}

class AllJobsSortByWithoutQuery {
    private _id: SortingActions  = new SortingActions('id');
    private _name: SortingActions  = new SortingActions('name');
    private _details: SortingActions  = new SortingActions('details');
    private _type: SortingActions  = new SortingActions('type');
    private _status: SortingActions  = new SortingActions('status');
    private _statusChanges: SortingActions  = new SortingActions('statusChanges');
    private _progress: SortingActions  = new SortingActions('progress');

    get id(): SortingActions {
        return this._id;
    }
    get name(): SortingActions {
        return this._name;
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
    get statusChanges(): SortingActions {
        return this._statusChanges;
    }
    get progress(): SortingActions {
        return this._progress;
    }
}

export interface AllJobsArguments {
    eventStore: string;
    namespace: string;
}
export class AllJobs extends ObservableQueryFor<Job[], AllJobsArguments> {
    readonly route: string = '/api/event-store/{eventStore}/{namespace}/jobs';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: Job[] = [];
    private readonly _sortBy: AllJobsSortBy;
    private static readonly _sortBy: AllJobsSortByWithoutQuery = new AllJobsSortByWithoutQuery();

    constructor() {
        super(Job, true);
        this._sortBy = new AllJobsSortBy(this);
    }

    get requiredRequestArguments(): string[] {
        return [
            'eventStore',
            'namespace',
        ];
    }

    get sortBy(): AllJobsSortBy {
        return this._sortBy;
    }

    static get sortBy(): AllJobsSortByWithoutQuery {
        return this._sortBy;
    }

    static use(args?: AllJobsArguments, sorting?: Sorting): [QueryResultWithState<Job[]>, SetSorting] {
        return useObservableQuery<Job[], AllJobs, AllJobsArguments>(AllJobs, args, sorting);
    }

    static useWithPaging(pageSize: number, args?: AllJobsArguments, sorting?: Sorting): [QueryResultWithState<Job[]>, SetSorting, SetPage, SetPageSize] {
        return useObservableQueryWithPaging<Job[], AllJobs>(AllJobs, new Paging(0, pageSize), args, sorting);
    }
}
