/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

/* eslint-disable sort-imports */
// eslint-disable-next-line header/header
import { QueryFor, QueryResultWithState, Sorting, SortingActions, SortingActionsForQuery, Paging } from '@cratis/applications/queries';
import { useQuery, useQueryWithPaging, PerformQuery, SetSorting, SetPage, SetPageSize } from '@cratis/applications.react/queries';
import { AppendedEventWithJsonAsContent } from './AppendedEventWithJsonAsContent';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/event-store/{{eventStore}}/{{namespace}}/sequence/{{eventSequenceId}}/range');

class AppendedEventsInRangeSortBy {
    private _metadata: SortingActionsForQuery<AppendedEventWithJsonAsContent[]>;
    private _context: SortingActionsForQuery<AppendedEventWithJsonAsContent[]>;
    private _content: SortingActionsForQuery<AppendedEventWithJsonAsContent[]>;

    constructor(readonly query: AppendedEventsInRange) {
        this._metadata = new SortingActionsForQuery<AppendedEventWithJsonAsContent[]>('metadata', query);
        this._context = new SortingActionsForQuery<AppendedEventWithJsonAsContent[]>('context', query);
        this._content = new SortingActionsForQuery<AppendedEventWithJsonAsContent[]>('content', query);
    }

    get metadata(): SortingActionsForQuery<AppendedEventWithJsonAsContent[]> {
        return this._metadata;
    }
    get context(): SortingActionsForQuery<AppendedEventWithJsonAsContent[]> {
        return this._context;
    }
    get content(): SortingActionsForQuery<AppendedEventWithJsonAsContent[]> {
        return this._content;
    }
}

class AppendedEventsInRangeSortByWithoutQuery {
    private _metadata: SortingActions  = new SortingActions('metadata');
    private _context: SortingActions  = new SortingActions('context');
    private _content: SortingActions  = new SortingActions('content');

    get metadata(): SortingActions {
        return this._metadata;
    }
    get context(): SortingActions {
        return this._context;
    }
    get content(): SortingActions {
        return this._content;
    }
}

export interface AppendedEventsInRangeArguments {
    eventStore: string;
    namespace: string;
    eventSequenceId: string;
    fromSequenceNumber: number;
    toSequenceNumber: number;
    eventSourceId?: string;
}

export class AppendedEventsInRange extends QueryFor<AppendedEventWithJsonAsContent[], AppendedEventsInRangeArguments> {
    readonly route: string = '/api/event-store/{eventStore}/{namespace}/sequence/{eventSequenceId}/range';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: AppendedEventWithJsonAsContent[] = [];
    private readonly _sortBy: AppendedEventsInRangeSortBy;
    private static readonly _sortBy: AppendedEventsInRangeSortByWithoutQuery = new AppendedEventsInRangeSortByWithoutQuery();

    constructor() {
        super(AppendedEventWithJsonAsContent, true);
        this._sortBy = new AppendedEventsInRangeSortBy(this);
    }

    get requiredRequestArguments(): string[] {
        return [
            'eventStore',
            'namespace',
            'eventSequenceId',
            'fromSequenceNumber',
            'toSequenceNumber',
        ];
    }

    get sortBy(): AppendedEventsInRangeSortBy {
        return this._sortBy;
    }

    static get sortBy(): AppendedEventsInRangeSortByWithoutQuery {
        return this._sortBy;
    }

    static use(args?: AppendedEventsInRangeArguments, sorting?: Sorting): [QueryResultWithState<AppendedEventWithJsonAsContent[]>, PerformQuery<AppendedEventsInRangeArguments>, SetSorting] {
        return useQuery<AppendedEventWithJsonAsContent[], AppendedEventsInRange, AppendedEventsInRangeArguments>(AppendedEventsInRange, args, sorting);
    }

    static useWithPaging(pageSize: number, args?: AppendedEventsInRangeArguments, sorting?: Sorting): [QueryResultWithState<AppendedEventWithJsonAsContent[]>, PerformQuery, SetSorting, SetPage, SetPageSize] {
        return useQueryWithPaging<AppendedEventWithJsonAsContent[], AppendedEventsInRange>(AppendedEventsInRange, new Paging(0, pageSize), args, sorting);
    }
}
