/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

/* eslint-disable sort-imports */
// eslint-disable-next-line header/header
import { QueryFor, QueryResultWithState, Sorting, SortingActions, SortingActionsForQuery, Paging } from '@cratis/applications/queries';
import { useQuery, useQueryWithPaging, PerformQuery, SetSorting, SetPage, SetPageSize } from '@cratis/applications.react/queries';
import { Projection } from './Projection';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/event-store/{{eventStore}}/projections');

class AllProjectionsSortBy {
    private _id: SortingActionsForQuery<Projection[]>;
    private _isActive: SortingActionsForQuery<Projection[]>;
    private _readModelName: SortingActionsForQuery<Projection[]>;

    constructor(readonly query: AllProjections) {
        this._id = new SortingActionsForQuery<Projection[]>('id', query);
        this._isActive = new SortingActionsForQuery<Projection[]>('isActive', query);
        this._readModelName = new SortingActionsForQuery<Projection[]>('readModelName', query);
    }

    get id(): SortingActionsForQuery<Projection[]> {
        return this._id;
    }
    get isActive(): SortingActionsForQuery<Projection[]> {
        return this._isActive;
    }
    get readModelName(): SortingActionsForQuery<Projection[]> {
        return this._readModelName;
    }
}

class AllProjectionsSortByWithoutQuery {
    private _id: SortingActions  = new SortingActions('id');
    private _isActive: SortingActions  = new SortingActions('isActive');
    private _readModelName: SortingActions  = new SortingActions('readModelName');

    get id(): SortingActions {
        return this._id;
    }
    get isActive(): SortingActions {
        return this._isActive;
    }
    get readModelName(): SortingActions {
        return this._readModelName;
    }
}

export interface AllProjectionsParameters {
    eventStore: string;
}

export class AllProjections extends QueryFor<Projection[], AllProjectionsParameters> {
    readonly route: string = '/api/event-store/{eventStore}/projections';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: Projection[] = [];
    private readonly _sortBy: AllProjectionsSortBy;
    private static readonly _sortBy: AllProjectionsSortByWithoutQuery = new AllProjectionsSortByWithoutQuery();

    constructor() {
        super(Projection, true);
        this._sortBy = new AllProjectionsSortBy(this);
    }

    get requiredRequestParameters(): string[] {
        return [
            'eventStore',
        ];
    }

    get sortBy(): AllProjectionsSortBy {
        return this._sortBy;
    }

    static get sortBy(): AllProjectionsSortByWithoutQuery {
        return this._sortBy;
    }

    static use(args?: AllProjectionsParameters, sorting?: Sorting): [QueryResultWithState<Projection[]>, PerformQuery<AllProjectionsParameters>, SetSorting] {
        return useQuery<Projection[], AllProjections, AllProjectionsParameters>(AllProjections, args, sorting);
    }

    static useWithPaging(pageSize: number, args?: AllProjectionsParameters, sorting?: Sorting): [QueryResultWithState<Projection[]>, PerformQuery, SetSorting, SetPage, SetPageSize] {
        return useQueryWithPaging<Projection[], AllProjections>(AllProjections, new Paging(0, pageSize), args, sorting);
    }
}
