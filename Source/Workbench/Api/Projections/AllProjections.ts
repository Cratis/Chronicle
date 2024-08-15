/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

// eslint-disable-next-line header/header
import { QueryFor, QueryResultWithState, Sorting, SortingActions, SortingActionsForQuery, Paging } from '@cratis/applications/queries';
import { useQuery, useQueryWithPaging, PerformQuery, SetSorting, SetPage, SetPageSize } from '@cratis/applications.react/queries';
import { Projection } from './Projection';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/{{eventStore}}/projections');

class AllProjectionsSortBy {
    private _id: SortingActionsForQuery<Projection[]>;
    private _isActive: SortingActionsForQuery<Projection[]>;
    private _modelName: SortingActionsForQuery<Projection[]>;

    constructor(readonly query: AllProjections) {
        this._id = new SortingActionsForQuery<Projection[]>('id', query);
        this._isActive = new SortingActionsForQuery<Projection[]>('isActive', query);
        this._modelName = new SortingActionsForQuery<Projection[]>('modelName', query);
    }

    get id(): SortingActionsForQuery<Projection[]> {
        return this._id;
    }
    get isActive(): SortingActionsForQuery<Projection[]> {
        return this._isActive;
    }
    get modelName(): SortingActionsForQuery<Projection[]> {
        return this._modelName;
    }
}

class AllProjectionsSortByWithoutQuery {
    private _id: SortingActions  = new SortingActions('id');
    private _isActive: SortingActions  = new SortingActions('isActive');
    private _modelName: SortingActions  = new SortingActions('modelName');

    get id(): SortingActions {
        return this._id;
    }
    get isActive(): SortingActions {
        return this._isActive;
    }
    get modelName(): SortingActions {
        return this._modelName;
    }
}

export interface AllProjectionsArguments {
    eventStore: string;
}

export class AllProjections extends QueryFor<Projection[], AllProjectionsArguments> {
    readonly route: string = '/api/events/store/{eventStore}/projections';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: Projection[] = [];
    private readonly _sortBy: AllProjectionsSortBy;
    private static readonly _sortBy: AllProjectionsSortByWithoutQuery = new AllProjectionsSortByWithoutQuery();

    constructor() {
        super(Projection, true);
        this._sortBy = new AllProjectionsSortBy(this);
    }

    get requestArguments(): string[] {
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

    static use(args?: AllProjectionsArguments, sorting?: Sorting): [QueryResultWithState<Projection[]>, PerformQuery<AllProjectionsArguments>, SetSorting] {
        return useQuery<Projection[], AllProjections, AllProjectionsArguments>(AllProjections, args, sorting);
    }

    static useWithPaging(pageSize: number, args?: AllProjectionsArguments, sorting?: Sorting): [QueryResultWithState<Projection[]>, PerformQuery, SetSorting, SetPage, SetPageSize] {
        return useQueryWithPaging<Projection[], AllProjections>(AllProjections, new Paging(0, pageSize), args, sorting);
    }
}
