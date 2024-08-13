/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

// eslint-disable-next-line header/header
import { QueryFor, QueryResultWithState, Sorting, SortingActions, SortingActionsForQuery, Paging } from '@cratis/applications/queries';
import { useQuery, useQueryWithPaging, PerformQuery, SetSorting, SetPage, SetPageSize } from '@cratis/applications.react/queries';
import { EventHistogramEntry } from './EventHistogramEntry';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/event-store/{{eventStore}}/{{namespace}}/sequence/{{eventSequenceId}}/histogram');

class HistogramSortBy {
    private _date: SortingActionsForQuery<EventHistogramEntry[]>;
    private _count: SortingActionsForQuery<EventHistogramEntry[]>;

    constructor(readonly query: Histogram) {
        this._date = new SortingActionsForQuery<EventHistogramEntry[]>('date', query);
        this._count = new SortingActionsForQuery<EventHistogramEntry[]>('count', query);
    }

    get date(): SortingActionsForQuery<EventHistogramEntry[]> {
        return this._date;
    }
    get count(): SortingActionsForQuery<EventHistogramEntry[]> {
        return this._count;
    }
}

class HistogramSortByWithoutQuery {
    private _date: SortingActions  = new SortingActions('date');
    private _count: SortingActions  = new SortingActions('count');

    get date(): SortingActions {
        return this._date;
    }
    get count(): SortingActions {
        return this._count;
    }
}

export class Histogram extends QueryFor<EventHistogramEntry[]> {
    readonly route: string = '/api/event-store/{eventStore}/{namespace}/sequence/{eventSequenceId}/histogram';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: EventHistogramEntry[] = [];
    private readonly _sortBy: HistogramSortBy;
    private static readonly _sortBy: HistogramSortByWithoutQuery = new HistogramSortByWithoutQuery();

    constructor() {
        super(EventHistogramEntry, true);
        this._sortBy = new HistogramSortBy(this);
    }

    get requestArguments(): string[] {
        return [
        ];
    }

    get sortBy(): HistogramSortBy {
        return this._sortBy;
    }

    static get sortBy(): HistogramSortByWithoutQuery {
        return this._sortBy;
    }

    static use(sorting?: Sorting): [QueryResultWithState<EventHistogramEntry[]>, PerformQuery, SetSorting] {
        return useQuery<EventHistogramEntry[], Histogram>(Histogram, undefined, sorting);
    }

    static useWithPaging(pageSize: number, sorting?: Sorting): [QueryResultWithState<EventHistogramEntry[]>, PerformQuery, SetSorting, SetPage, SetPageSize] {
        return useQueryWithPaging<EventHistogramEntry[], Histogram>(Histogram, new Paging(0, pageSize), undefined, sorting);
    }
}
