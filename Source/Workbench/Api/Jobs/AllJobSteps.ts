/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

// eslint-disable-next-line header/header
import { ObservableQueryFor, QueryResultWithState, Sorting, SortingActions, SortingActionsForObservableQuery, Paging } from '@cratis/applications/queries';
import { useObservableQuery, useObservableQueryWithPaging, SetSorting, SetPage, SetPageSize } from '@cratis/applications.react/queries';
import { JobStepState } from './JobStepState';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/event-store/{{eventStore}}/{{namespace}}/jobs/{{jobId}}/steps');

class AllJobStepsSortBy {
    private _id: SortingActionsForObservableQuery<JobStepState[]>;
    private _type: SortingActionsForObservableQuery<JobStepState[]>;
    private _name: SortingActionsForObservableQuery<JobStepState[]>;
    private _status: SortingActionsForObservableQuery<JobStepState[]>;
    private _statusChanges: SortingActionsForObservableQuery<JobStepState[]>;
    private _progress: SortingActionsForObservableQuery<JobStepState[]>;

    constructor(readonly query: AllJobSteps) {
        this._id = new SortingActionsForObservableQuery<JobStepState[]>('id', query);
        this._type = new SortingActionsForObservableQuery<JobStepState[]>('type', query);
        this._name = new SortingActionsForObservableQuery<JobStepState[]>('name', query);
        this._status = new SortingActionsForObservableQuery<JobStepState[]>('status', query);
        this._statusChanges = new SortingActionsForObservableQuery<JobStepState[]>('statusChanges', query);
        this._progress = new SortingActionsForObservableQuery<JobStepState[]>('progress', query);
    }

    get id(): SortingActionsForObservableQuery<JobStepState[]> {
        return this._id;
    }
    get type(): SortingActionsForObservableQuery<JobStepState[]> {
        return this._type;
    }
    get name(): SortingActionsForObservableQuery<JobStepState[]> {
        return this._name;
    }
    get status(): SortingActionsForObservableQuery<JobStepState[]> {
        return this._status;
    }
    get statusChanges(): SortingActionsForObservableQuery<JobStepState[]> {
        return this._statusChanges;
    }
    get progress(): SortingActionsForObservableQuery<JobStepState[]> {
        return this._progress;
    }
}

class AllJobStepsSortByWithoutQuery {
    private _id: SortingActions  = new SortingActions('id');
    private _type: SortingActions  = new SortingActions('type');
    private _name: SortingActions  = new SortingActions('name');
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

export interface AllJobStepsArguments {
    eventStore: string;
    namespace: string;
    jobId: string;
}
export class AllJobSteps extends ObservableQueryFor<JobStepState[], AllJobStepsArguments> {
    readonly route: string = '/api/event-store/{eventStore}/{namespace}/jobs/{jobId}/steps';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: JobStepState[] = [];
    private readonly _sortBy: AllJobStepsSortBy;
    private static readonly _sortBy: AllJobStepsSortByWithoutQuery = new AllJobStepsSortByWithoutQuery();

    constructor() {
        super(JobStepState, true);
        this._sortBy = new AllJobStepsSortBy(this);
    }

    get requestArguments(): string[] {
        return [
            'eventStore',
            'namespace',
            'jobId',
        ];
    }

    get sortBy(): AllJobStepsSortBy {
        return this._sortBy;
    }

    static get sortBy(): AllJobStepsSortByWithoutQuery {
        return this._sortBy;
    }

    static use(args?: AllJobStepsArguments, sorting?: Sorting): [QueryResultWithState<JobStepState[]>, SetSorting] {
        return useObservableQuery<JobStepState[], AllJobSteps, AllJobStepsArguments>(AllJobSteps, args, sorting);
    }

    static useWithPaging(pageSize: number, args?: AllJobStepsArguments, sorting?: Sorting): [QueryResultWithState<JobStepState[]>, SetSorting, SetPage, SetPageSize] {
        return useObservableQueryWithPaging<JobStepState[], AllJobSteps>(AllJobSteps, new Paging(0, pageSize), args, sorting);
    }
}
