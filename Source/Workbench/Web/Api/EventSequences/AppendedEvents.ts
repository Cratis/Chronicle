/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

/* eslint-disable sort-imports */
// eslint-disable-next-line header/header
import { QueryFor, QueryResultWithState, Sorting, SortingActions, SortingActionsForQuery, Paging } from '@cratis/applications/queries';
import { useQuery, useQueryWithPaging, PerformQuery, SetSorting, SetPage, SetPageSize } from '@cratis/applications.react/queries';
import { AppendedEventWithJsonAsContent } from './AppendedEventWithJsonAsContent';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/event-store/{{eventStore}}/{{namespace}}/sequence/{{eventSequenceId}}');

class AppendedEventsSortBy {
    private _metadata: SortingActionsForQuery<AppendedEventWithJsonAsContent[]>;
    private _context: SortingActionsForQuery<AppendedEventWithJsonAsContent[]>;
    private _content: SortingActionsForQuery<AppendedEventWithJsonAsContent[]>;

    constructor(readonly query: AppendedEvents) {
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

class AppendedEventsSortByWithoutQuery {
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

export interface AppendedEventsArguments {
    eventStore: string;
    namespace: string;
    eventSequenceId: string;
    eventSourceId?: string;
}

export class AppendedEvents extends QueryFor<AppendedEventWithJsonAsContent[], AppendedEventsArguments> {
    readonly route: string = '/api/event-store/{eventStore}/{namespace}/sequence/{eventSequenceId}';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: AppendedEventWithJsonAsContent[] = [];
    private readonly _sortBy: AppendedEventsSortBy;
    private static readonly _sortBy: AppendedEventsSortByWithoutQuery = new AppendedEventsSortByWithoutQuery();

    constructor() {
        super(AppendedEventWithJsonAsContent, true);
        this._sortBy = new AppendedEventsSortBy(this);
    }

    get requiredRequestArguments(): string[] {
        return [
            'eventStore',
            'namespace',
            'eventSequenceId',
        ];
    }

    get sortBy(): AppendedEventsSortBy {
        return this._sortBy;
    }

    static get sortBy(): AppendedEventsSortByWithoutQuery {
        return this._sortBy;
    }

    static use(args?: AppendedEventsArguments, sorting?: Sorting): [QueryResultWithState<AppendedEventWithJsonAsContent[]>, PerformQuery<AppendedEventsArguments>, SetSorting] {
        return useQuery<AppendedEventWithJsonAsContent[], AppendedEvents, AppendedEventsArguments>(AppendedEvents, args, sorting);
    }

    static useWithPaging(pageSize: number, args?: AppendedEventsArguments, sorting?: Sorting): [QueryResultWithState<AppendedEventWithJsonAsContent[]>, PerformQuery, SetSorting, SetPage, SetPageSize] {
        return useQueryWithPaging<AppendedEventWithJsonAsContent[], AppendedEvents>(AppendedEvents, new Paging(0, pageSize), args, sorting);
    }
}
