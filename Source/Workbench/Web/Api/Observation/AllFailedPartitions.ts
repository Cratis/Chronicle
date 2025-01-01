/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

/* eslint-disable sort-imports */
// eslint-disable-next-line header/header
import { ObservableQueryFor, QueryResultWithState, Sorting, SortingActions, SortingActionsForObservableQuery, Paging } from '@cratis/applications/queries';
import { useObservableQuery, useObservableQueryWithPaging, SetSorting, SetPage, SetPageSize } from '@cratis/applications.react/queries';
import { FailedPartition } from '../Contracts/Observation/FailedPartition';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/event-store/{{eventStore}}/{{namespace}}/failed-partitions/{{observerId?}}');

class AllFailedPartitionsSortBy {
    private _id: SortingActionsForObservableQuery<FailedPartition[]>;
    private _partition: SortingActionsForObservableQuery<FailedPartition[]>;
    private _attempts: SortingActionsForObservableQuery<FailedPartition[]>;

    constructor(readonly query: AllFailedPartitions) {
        this._id = new SortingActionsForObservableQuery<FailedPartition[]>('id', query);
        this._partition = new SortingActionsForObservableQuery<FailedPartition[]>('partition', query);
        this._attempts = new SortingActionsForObservableQuery<FailedPartition[]>('attempts', query);
    }

    get id(): SortingActionsForObservableQuery<FailedPartition[]> {
        return this._id;
    }
    get partition(): SortingActionsForObservableQuery<FailedPartition[]> {
        return this._partition;
    }
    get attempts(): SortingActionsForObservableQuery<FailedPartition[]> {
        return this._attempts;
    }
}

class AllFailedPartitionsSortByWithoutQuery {
    private _id: SortingActions  = new SortingActions('id');
    private _partition: SortingActions  = new SortingActions('partition');
    private _attempts: SortingActions  = new SortingActions('attempts');

    get id(): SortingActions {
        return this._id;
    }
    get partition(): SortingActions {
        return this._partition;
    }
    get attempts(): SortingActions {
        return this._attempts;
    }
}

export interface AllFailedPartitionsArguments {
    eventStore: string;
    namespace: string;
    observerId?: string;
}
export class AllFailedPartitions extends ObservableQueryFor<FailedPartition[], AllFailedPartitionsArguments> {
    readonly route: string = '/api/event-store/{eventStore}/{namespace}/failed-partitions/{observerId?}';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: FailedPartition[] = [];
    private readonly _sortBy: AllFailedPartitionsSortBy;
    private static readonly _sortBy: AllFailedPartitionsSortByWithoutQuery = new AllFailedPartitionsSortByWithoutQuery();

    constructor() {
        super(FailedPartition, true);
        this._sortBy = new AllFailedPartitionsSortBy(this);
    }

    get requiredRequestArguments(): string[] {
        return [
            'eventStore',
            'namespace',
        ];
    }

    get sortBy(): AllFailedPartitionsSortBy {
        return this._sortBy;
    }

    static get sortBy(): AllFailedPartitionsSortByWithoutQuery {
        return this._sortBy;
    }

    static use(args?: AllFailedPartitionsArguments, sorting?: Sorting): [QueryResultWithState<FailedPartition[]>, SetSorting] {
        return useObservableQuery<FailedPartition[], AllFailedPartitions, AllFailedPartitionsArguments>(AllFailedPartitions, args, sorting);
    }

    static useWithPaging(pageSize: number, args?: AllFailedPartitionsArguments, sorting?: Sorting): [QueryResultWithState<FailedPartition[]>, SetSorting, SetPage, SetPageSize] {
        return useObservableQueryWithPaging<FailedPartition[], AllFailedPartitions>(AllFailedPartitions, new Paging(0, pageSize), args, sorting);
    }
}
