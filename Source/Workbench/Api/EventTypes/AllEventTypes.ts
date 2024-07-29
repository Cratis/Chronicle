/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

// eslint-disable-next-line header/header
import { QueryFor, QueryResultWithState, Sorting, SortingActions, SortingActionsForQuery, Paging } from '@cratis/applications/queries';
import { useQuery, useQueryWithPaging, PerformQuery, SetSorting, SetPage, SetPageSize } from '@cratis/applications.react/queries';
import { EventType } from './EventType';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/{eventStore}/types');

class AllEventTypesSortBy {
    private _id: SortingActionsForQuery<EventType[]>;
    private _generation: SortingActionsForQuery<EventType[]>;
    private _isPublic: SortingActionsForQuery<EventType[]>;

    constructor(readonly query: AllEventTypes) {
        this._id = new SortingActionsForQuery<EventType[]>('id', query);
        this._generation = new SortingActionsForQuery<EventType[]>('generation', query);
        this._isPublic = new SortingActionsForQuery<EventType[]>('isPublic', query);
    }

    get id(): SortingActionsForQuery<EventType[]> {
        return this._id;
    }
    get generation(): SortingActionsForQuery<EventType[]> {
        return this._generation;
    }
    get isPublic(): SortingActionsForQuery<EventType[]> {
        return this._isPublic;
    }
}

class AllEventTypesSortByWithoutQuery {
    private _id: SortingActions  = new SortingActions('id');
    private _generation: SortingActions  = new SortingActions('generation');
    private _isPublic: SortingActions  = new SortingActions('isPublic');

    get id(): SortingActions {
        return this._id;
    }
    get generation(): SortingActions {
        return this._generation;
    }
    get isPublic(): SortingActions {
        return this._isPublic;
    }
}

export interface AllEventTypesArguments {
    eventStore: string;
}

export class AllEventTypes extends QueryFor<EventType[], AllEventTypesArguments> {
    readonly route: string = '/api/events/store/{eventStore}/types';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: EventType[] = [];
    private readonly _sortBy: AllEventTypesSortBy;
    private static readonly _sortBy: AllEventTypesSortByWithoutQuery = new AllEventTypesSortByWithoutQuery();

    constructor() {
        super(EventType, true);
        this._sortBy = new AllEventTypesSortBy(this);
    }

    get requestArguments(): string[] {
        return [
            'eventStore',
        ];
    }

    get sortBy(): AllEventTypesSortBy {
        return this._sortBy;
    }

    static get sortBy(): AllEventTypesSortByWithoutQuery {
        return this._sortBy;
    }

    static use(args?: AllEventTypesArguments, sorting?: Sorting): [QueryResultWithState<EventType[]>, PerformQuery<AllEventTypesArguments>, SetSorting] {
        return useQuery<EventType[], AllEventTypes, AllEventTypesArguments>(AllEventTypes, args, sorting);
    }

    static useWithPaging(pageSize: number, args?: AllEventTypesArguments, sorting?: Sorting): [QueryResultWithState<EventType[]>, PerformQuery, SetSorting, SetPage, SetPageSize] {
        return useQueryWithPaging<EventType[], AllEventTypes>(AllEventTypes, new Paging(0, pageSize), args, sorting);
    }
}
