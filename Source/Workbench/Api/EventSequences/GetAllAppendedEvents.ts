/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

// eslint-disable-next-line header/header
import { QueryFor, QueryResultWithState, Sorting, SortingActions, SortingActionsForQuery, Paging } from '@cratis/applications/queries';
import { useQuery, useQueryWithPaging, PerformQuery, SetSorting, SetPage, SetPageSize } from '@cratis/applications.react/queries';
import { AppendedEventWithJsonAsContent } from './AppendedEventWithJsonAsContent';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/event-store/{{eventStore}}/{{namespace}}/sequence/{{eventSequenceId}}/all');

class GetAllAppendedEventsSortBy {
    private _metadata: SortingActionsForQuery<AppendedEventWithJsonAsContent[]>;
    private _context: SortingActionsForQuery<AppendedEventWithJsonAsContent[]>;
    private _content: SortingActionsForQuery<AppendedEventWithJsonAsContent[]>;

    constructor(readonly query: GetAllAppendedEvents) {
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

class GetAllAppendedEventsSortByWithoutQuery {
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

export interface GetAllAppendedEventsArguments {
    eventStore: string;
    namespace: string;
    eventSequenceId: string;
    eventSourceId?: string;
}

export class GetAllAppendedEvents extends QueryFor<AppendedEventWithJsonAsContent[], GetAllAppendedEventsArguments> {
    readonly route: string = '/api/event-store/{eventStore}/{namespace}/sequence/{eventSequenceId}/all';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: AppendedEventWithJsonAsContent[] = [];
    private readonly _sortBy: GetAllAppendedEventsSortBy;
    private static readonly _sortBy: GetAllAppendedEventsSortByWithoutQuery = new GetAllAppendedEventsSortByWithoutQuery();

    constructor() {
        super(AppendedEventWithJsonAsContent, true);
        this._sortBy = new GetAllAppendedEventsSortBy(this);
    }

    get requestArguments(): string[] {
        return [
            'eventStore',
            'namespace',
            'eventSequenceId',
            'eventSourceId',
        ];
    }

    get sortBy(): GetAllAppendedEventsSortBy {
        return this._sortBy;
    }

    static get sortBy(): GetAllAppendedEventsSortByWithoutQuery {
        return this._sortBy;
    }

    static use(args?: GetAllAppendedEventsArguments, sorting?: Sorting): [QueryResultWithState<AppendedEventWithJsonAsContent[]>, PerformQuery<GetAllAppendedEventsArguments>, SetSorting] {
        return useQuery<AppendedEventWithJsonAsContent[], GetAllAppendedEvents, GetAllAppendedEventsArguments>(GetAllAppendedEvents, args, sorting);
    }

    static useWithPaging(pageSize: number, args?: GetAllAppendedEventsArguments, sorting?: Sorting): [QueryResultWithState<AppendedEventWithJsonAsContent[]>, PerformQuery, SetSorting, SetPage, SetPageSize] {
        return useQueryWithPaging<AppendedEventWithJsonAsContent[], GetAllAppendedEvents>(GetAllAppendedEvents, new Paging(0, pageSize), args, sorting);
    }
}
