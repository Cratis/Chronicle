/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { QueryFor, QueryResultWithState, useQuery, PerformQuery } from '@aksio/applications/queries';
import { PagedQueryResult } from './PagedQueryResult';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/{{microserviceId}}/{{tenantId}}/sequence/{{eventSequenceId}}?fromSequenceNumber={{fromSequenceNumber}}&toSequenceNumber={{toSequenceNumber}}&eventSourceId={{eventSourceId}}&eventTypes={{eventTypes}}');

export interface GetAppendedEventsRangeArguments {
    eventSequenceId: string;
    microserviceId: string;
    tenantId: string;
    fromSequenceNumber: number;
    toSequenceNumber: number;
    eventSourceId: string;
    eventTypes: any;
}
export class GetAppendedEventsRange extends QueryFor<PagedQueryResult, GetAppendedEventsRangeArguments> {
    readonly route: string = '/api/events/store/{{microserviceId}}/{{tenantId}}/sequence/{{eventSequenceId}}?fromSequenceNumber={{fromSequenceNumber}}&toSequenceNumber={{toSequenceNumber}}&eventSourceId={{eventSourceId}}&eventTypes={{eventTypes}}';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: PagedQueryResult = {} as any;

    constructor() {
        super(PagedQueryResult, false);
    }

    get requestArguments(): string[] {
        return [
            'eventSequenceId',
            'microserviceId',
            'tenantId',
            'fromSequenceNumber',
            'toSequenceNumber',
            'eventSourceId',
            'eventTypes',
        ];
    }

    static use(args?: GetAppendedEventsRangeArguments): [QueryResultWithState<PagedQueryResult>, PerformQuery<GetAppendedEventsRangeArguments>] {
        return useQuery<PagedQueryResult, GetAppendedEventsRange, GetAppendedEventsRangeArguments>(GetAppendedEventsRange, args);
    }
}
