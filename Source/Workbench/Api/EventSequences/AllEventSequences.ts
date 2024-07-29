/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

// eslint-disable-next-line header/header
import { QueryFor, QueryResultWithState, Sorting, SortingActions, SortingActionsForQuery, Paging } from '@cratis/applications/queries';
import { useQuery, useQueryWithPaging, PerformQuery, SetSorting, SetPage, SetPageSize } from '@cratis/applications.react/queries';
import { EventSequenceInformation } from './EventSequenceInformation';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/sequences');

class AllEventSequencesSortBy {
    private _id: SortingActionsForQuery<EventSequenceInformation[]>;
    private _name: SortingActionsForQuery<EventSequenceInformation[]>;

    constructor(readonly query: AllEventSequences) {
        this._id = new SortingActionsForQuery<EventSequenceInformation[]>('id', query);
        this._name = new SortingActionsForQuery<EventSequenceInformation[]>('name', query);
    }

    get id(): SortingActionsForQuery<EventSequenceInformation[]> {
        return this._id;
    }
    get name(): SortingActionsForQuery<EventSequenceInformation[]> {
        return this._name;
    }
}

class AllEventSequencesSortByWithoutQuery {
    private _id: SortingActions  = new SortingActions('id');
    private _name: SortingActions  = new SortingActions('name');

    get id(): SortingActions {
        return this._id;
    }
    get name(): SortingActions {
        return this._name;
    }
}

export class AllEventSequences extends QueryFor<EventSequenceInformation[]> {
    readonly route: string = '/api/events/store/sequences';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: EventSequenceInformation[] = [];
    private readonly _sortBy: AllEventSequencesSortBy;
    private static readonly _sortBy: AllEventSequencesSortByWithoutQuery = new AllEventSequencesSortByWithoutQuery();

    constructor() {
        super(EventSequenceInformation, true);
        this._sortBy = new AllEventSequencesSortBy(this);
    }

    get requestArguments(): string[] {
        return [
        ];
    }

    get sortBy(): AllEventSequencesSortBy {
        return this._sortBy;
    }

    static get sortBy(): AllEventSequencesSortByWithoutQuery {
        return this._sortBy;
    }

    static use(sorting?: Sorting): [QueryResultWithState<EventSequenceInformation[]>, PerformQuery, SetSorting] {
        return useQuery<EventSequenceInformation[], AllEventSequences>(AllEventSequences, undefined, sorting);
    }

    static useWithPaging(pageSize: number, sorting?: Sorting): [QueryResultWithState<EventSequenceInformation[]>, PerformQuery, SetSorting, SetPage, SetPageSize] {
        return useQueryWithPaging<EventSequenceInformation[], AllEventSequences>(AllEventSequences, new Paging(0, pageSize), undefined, sorting);
    }
}
