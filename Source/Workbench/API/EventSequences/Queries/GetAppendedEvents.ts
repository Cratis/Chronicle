/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { QueryFor, QueryResultWithState, useQuery, PerformQuery } from 'Infrastructure/queries';
import { PagedQueryResult`1 } from '../../PagedQueryResult`1';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/{eventStore}/{namespace}/sequence/{eventSequenceId}');

export interface GetAppendedEventsArguments {
    eventStore: string;
    namespace: string;
    eventSequenceId: string;
    pageSize?: number;
    pageNumber?: number;
    eventSourceId?: string;
}
export class GetAppendedEvents extends QueryFor<PagedQueryResult`1, GetAppendedEventsArguments> {
    readonly route: string = '/api/events/store/{eventStore}/{namespace}/sequence/{eventSequenceId}';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: PagedQueryResult`1 = {} as any;

    constructor() {
        super(PagedQueryResult`1, false);
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

    static use(args?: GetAppendedEventsArguments): [QueryResultWithState<PagedQueryResult`1>, PerformQuery<GetAppendedEventsArguments>] {
        return useQuery<PagedQueryResult`1, GetAppendedEvents, GetAppendedEventsArguments>(GetAppendedEvents, args);
    }
}
