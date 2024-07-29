/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

// eslint-disable-next-line header/header
import { ObservableQueryFor, QueryResultWithState, Sorting, SortingActions, SortingActionsForObservableQuery, Paging } from '@cratis/applications/queries';
import { useObservableQuery, useObservableQueryWithPaging, SetSorting, SetPage, SetPageSize } from '@cratis/applications.react/queries';
import { JobState } from './JobState';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/{eventStore}/{namespace}/jobs');

class AllJobsSortBy {
    private _id: SortingActionsForObservableQuery<JobState[]>;
    private _type: SortingActionsForObservableQuery<JobState[]>;
    private _name: SortingActionsForObservableQuery<JobState[]>;
    private _details: SortingActionsForObservableQuery<JobState[]>;
    private _status: SortingActionsForObservableQuery<JobState[]>;
    private _statusChanges: SortingActionsForObservableQuery<JobState[]>;
    private _progress: SortingActionsForObservableQuery<JobState[]>;

    constructor(readonly query: AllJobs) {
        this._id = new SortingActionsForObservableQuery<JobState[]>('id', query);
        this._type = new SortingActionsForObservableQuery<JobState[]>('type', query);
        this._name = new SortingActionsForObservableQuery<JobState[]>('name', query);
        this._details = new SortingActionsForObservableQuery<JobState[]>('details', query);
        this._status = new SortingActionsForObservableQuery<JobState[]>('status', query);
        this._statusChanges = new SortingActionsForObservableQuery<JobState[]>('statusChanges', query);
        this._progress = new SortingActionsForObservableQuery<JobState[]>('progress', query);
    }

    get id(): SortingActionsForObservableQuery<JobState[]> {
        return this._id;
    }
    get type(): SortingActionsForObservableQuery<JobState[]> {
        return this._type;
    }
    get name(): SortingActionsForObservableQuery<JobState[]> {
        return this._name;
    }
    get details(): SortingActionsForObservableQuery<JobState[]> {
        return this._details;
    }
    get status(): SortingActionsForObservableQuery<JobState[]> {
        return this._status;
    }
    get statusChanges(): SortingActionsForObservableQuery<JobState[]> {
        return this._statusChanges;
    }
    get progress(): SortingActionsForObservableQuery<JobState[]> {
        return this._progress;
    }
}

class AllJobsSortByWithoutQuery {
    private _id: SortingActions  = new SortingActions('id');
    private _type: SortingActions  = new SortingActions('type');
    private _name: SortingActions  = new SortingActions('name');
    private _details: SortingActions  = new SortingActions('details');
    private _status: SortingActions  = new SortingActions('status');
    private _statusChanges: SortingActions  = new SortingActions('statusChanges');
    private _progress: SortingActions  = new SortingActions('progress');

    get id(): SortingActions {
        return this._id;
    }
    get type(): SortingActions {
        return this._type;
    }
    get name(): SortingActions {
        return this._name;
    }
    get details(): SortingActions {
        return this._details;
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
export class AllJobs extends ObservableQueryFor<JobState[], AllJobsArguments> {
    readonly route: string = '/api/events/store/{eventStore}/{namespace}/jobs';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: JobState[] = [];
    private readonly _sortBy: AllJobsSortBy;
    private static readonly _sortBy: AllJobsSortByWithoutQuery = new AllJobsSortByWithoutQuery();

    constructor() {
        super(JobState, true);
        this._sortBy = new AllJobsSortBy(this);
    }

    get requestArguments(): string[] {
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

    static use(args?: AllJobsArguments, sorting?: Sorting): [QueryResultWithState<JobState[]>, SetSorting] {
        return useObservableQuery<JobState[], AllJobs, AllJobsArguments>(AllJobs, args, sorting);
    }

    static useWithPaging(pageSize: number, args?: AllJobsArguments, sorting?: Sorting): [QueryResultWithState<JobState[]>, SetSorting, SetPage, SetPageSize] {
        return useObservableQueryWithPaging<JobState[], AllJobs>(AllJobs, new Paging(0, pageSize), args, sorting);
    }
}
