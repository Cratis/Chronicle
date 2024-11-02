/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

/* eslint-disable sort-imports */
// eslint-disable-next-line header/header
import { ObservableQueryFor, QueryResultWithState, Sorting, SortingActions, SortingActionsForObservableQuery, Paging } from '@cratis/applications/queries';
import { useObservableQuery, useObservableQueryWithPaging, SetSorting, SetPage, SetPageSize } from '@cratis/applications.react/queries';
import { JobInformation } from './JobInformation';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/event-store/{{eventStore}}/{{namespace}}/jobs');

class AllJobsSortBy {
    private _id: SortingActionsForObservableQuery<JobInformation[]>;
    private _type: SortingActionsForObservableQuery<JobInformation[]>;
    private _name: SortingActionsForObservableQuery<JobInformation[]>;
    private _details: SortingActionsForObservableQuery<JobInformation[]>;
    private _status: SortingActionsForObservableQuery<JobInformation[]>;
    private _statusChanges: SortingActionsForObservableQuery<JobInformation[]>;
    private _progress: SortingActionsForObservableQuery<JobInformation[]>;

    constructor(readonly query: AllJobs) {
        this._id = new SortingActionsForObservableQuery<JobInformation[]>('id', query);
        this._type = new SortingActionsForObservableQuery<JobInformation[]>('type', query);
        this._name = new SortingActionsForObservableQuery<JobInformation[]>('name', query);
        this._details = new SortingActionsForObservableQuery<JobInformation[]>('details', query);
        this._status = new SortingActionsForObservableQuery<JobInformation[]>('status', query);
        this._statusChanges = new SortingActionsForObservableQuery<JobInformation[]>('statusChanges', query);
        this._progress = new SortingActionsForObservableQuery<JobInformation[]>('progress', query);
    }

    get id(): SortingActionsForObservableQuery<JobInformation[]> {
        return this._id;
    }
    get type(): SortingActionsForObservableQuery<JobInformation[]> {
        return this._type;
    }
    get name(): SortingActionsForObservableQuery<JobInformation[]> {
        return this._name;
    }
    get details(): SortingActionsForObservableQuery<JobInformation[]> {
        return this._details;
    }
    get status(): SortingActionsForObservableQuery<JobInformation[]> {
        return this._status;
    }
    get statusChanges(): SortingActionsForObservableQuery<JobInformation[]> {
        return this._statusChanges;
    }
    get progress(): SortingActionsForObservableQuery<JobInformation[]> {
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
export class AllJobs extends ObservableQueryFor<JobInformation[], AllJobsArguments> {
    readonly route: string = '/api/event-store/{eventStore}/{namespace}/jobs';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: JobInformation[] = [];
    private readonly _sortBy: AllJobsSortBy;
    private static readonly _sortBy: AllJobsSortByWithoutQuery = new AllJobsSortByWithoutQuery();

    constructor() {
        super(JobInformation, true);
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

    static use(args?: AllJobsArguments, sorting?: Sorting): [QueryResultWithState<JobInformation[]>, SetSorting] {
        return useObservableQuery<JobInformation[], AllJobs, AllJobsArguments>(AllJobs, args, sorting);
    }

    static useWithPaging(pageSize: number, args?: AllJobsArguments, sorting?: Sorting): [QueryResultWithState<JobInformation[]>, SetSorting, SetPage, SetPageSize] {
        return useObservableQueryWithPaging<JobInformation[], AllJobs>(AllJobs, new Paging(0, pageSize), args, sorting);
    }
}
