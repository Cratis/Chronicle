/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

// eslint-disable-next-line header/header
import { QueryFor, QueryResultWithState, Sorting, SortingActions, SortingActionsForQuery, Paging } from '@cratis/applications/queries';
import { useQuery, useQueryWithPaging, PerformQuery, SetSorting, SetPage, SetPageSize } from '@cratis/applications.react/queries';
import { AppendedEventWithJsonAsContent } from './AppendedEventWithJsonAsContent';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/{{eventStore}}/{{namespace}}/sequence/{{eventSequenceId}}');

class GetAppendedEventsSortBy {
    private _metadata: SortingActionsForQuery<AppendedEventWithJsonAsContent[]>;
    private _context: SortingActionsForQuery<AppendedEventWithJsonAsContent[]>;
    private _content: SortingActionsForQuery<AppendedEventWithJsonAsContent[]>;

    constructor(readonly query: GetAppendedEvents) {
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

class GetAppendedEventsSortByWithoutQuery {
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

export interface GetAppendedEventsArguments {
    eventStore: string;
    namespace: string;
    eventSequenceId: string;
    pageSize?: number;
    pageNumber?: number;
    eventSourceId?: string;
}

export class GetAppendedEvents extends QueryFor<AppendedEventWithJsonAsContent[], GetAppendedEventsArguments> {
    readonly route: string = '/api/events/store/{eventStore}/{namespace}/sequence/{eventSequenceId}';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: AppendedEventWithJsonAsContent[] = [];
    private readonly _sortBy: GetAppendedEventsSortBy;
    private static readonly _sortBy: GetAppendedEventsSortByWithoutQuery = new GetAppendedEventsSortByWithoutQuery();

    constructor() {
        super(AppendedEventWithJsonAsContent, true);
        this._sortBy = new GetAppendedEventsSortBy(this);
    }

    get requestArguments(): string[] {
        return [
            'eventStore',
            'namespace',
            'eventSequenceId',
            'pageSize',
            'pageNumber',
            'eventSourceId',
        ];
    }

    get sortBy(): GetAppendedEventsSortBy {
        return this._sortBy;
    }

    static get sortBy(): GetAppendedEventsSortByWithoutQuery {
        return this._sortBy;
    }

    static use(args?: GetAppendedEventsArguments, sorting?: Sorting): [QueryResultWithState<AppendedEventWithJsonAsContent[]>, PerformQuery<GetAppendedEventsArguments>, SetSorting] {
        return useQuery<AppendedEventWithJsonAsContent[], GetAppendedEvents, GetAppendedEventsArguments>(GetAppendedEvents, args, sorting);
    }

    static useWithPaging(pageSize: number, args?: GetAppendedEventsArguments, sorting?: Sorting): [QueryResultWithState<AppendedEventWithJsonAsContent[]>, PerformQuery, SetSorting, SetPage, SetPageSize] {
        return useQueryWithPaging<AppendedEventWithJsonAsContent[], GetAppendedEvents>(GetAppendedEvents, new Paging(0, pageSize), args, sorting);
    }
}
