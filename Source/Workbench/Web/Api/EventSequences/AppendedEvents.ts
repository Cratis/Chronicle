/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

/* eslint-disable sort-imports */
// eslint-disable-next-line header/header
import { QueryFor, QueryResultWithState, Sorting, SortingActions, SortingActionsForQuery, Paging } from '@cratis/applications/queries';
import { useQuery, useQueryWithPaging, PerformQuery, SetSorting, SetPage, SetPageSize } from '@cratis/applications.react/queries';
import { AppendedEvent } from '../Events/AppendedEvent';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/event-store/{{eventStore}}/{{namespace}}/sequence/{{eventSequenceId}}?eventSourceId={{eventSourceId}}');

class AppendedEventsSortBy {
    private _metadata: SortingActionsForQuery<AppendedEvent[]>;
    private _context: SortingActionsForQuery<AppendedEvent[]>;
    private _content: SortingActionsForQuery<AppendedEvent[]>;

    constructor(readonly query: AppendedEvents) {
        this._metadata = new SortingActionsForQuery<AppendedEvent[]>('metadata', query);
        this._context = new SortingActionsForQuery<AppendedEvent[]>('context', query);
        this._content = new SortingActionsForQuery<AppendedEvent[]>('content', query);
    }

    get metadata(): SortingActionsForQuery<AppendedEvent[]> {
        return this._metadata;
    }
    get context(): SortingActionsForQuery<AppendedEvent[]> {
        return this._context;
    }
    get content(): SortingActionsForQuery<AppendedEvent[]> {
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

export class AppendedEvents extends QueryFor<AppendedEvent[], AppendedEventsArguments> {
    readonly route: string = '/api/event-store/{eventStore}/{namespace}/sequence/{eventSequenceId}?eventSourceId={eventSourceId}';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: AppendedEvent[] = [];
    private readonly _sortBy: AppendedEventsSortBy;
    private static readonly _sortBy: AppendedEventsSortByWithoutQuery = new AppendedEventsSortByWithoutQuery();

    constructor() {
        super(AppendedEvent, true);
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

    static use(args?: AppendedEventsArguments, sorting?: Sorting): [QueryResultWithState<AppendedEvent[]>, PerformQuery<AppendedEventsArguments>, SetSorting] {
        return useQuery<AppendedEvent[], AppendedEvents, AppendedEventsArguments>(AppendedEvents, args, sorting);
    }

    static useWithPaging(pageSize: number, args?: AppendedEventsArguments, sorting?: Sorting): [QueryResultWithState<AppendedEvent[]>, PerformQuery, SetSorting, SetPage, SetPageSize] {
        return useQueryWithPaging<AppendedEvent[], AppendedEvents>(AppendedEvents, new Paging(0, pageSize), args, sorting);
    }
}
