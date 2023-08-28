/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { QueryFor, QueryResultWithState, useQuery, PerformQuery } from '@aksio/applications/queries';
import { PagedQueryResult } from './PagedQueryResult';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/{{microserviceId}}/{{tenantId}}/sequence/{{eventSequenceId}}?pageSize={{pageSize}}&pageNumber={{pageNumber}}');

export interface GetAppendedEventsArguments {
    eventSequenceId: string;
    microserviceId: string;
    tenantId: string;
    pageSize: number;
    pageNumber: number;
}
export class GetAppendedEvents extends QueryFor<PagedQueryResult, GetAppendedEventsArguments> {
    readonly route: string = '/api/events/store/{{microserviceId}}/{{tenantId}}/sequence/{{eventSequenceId}}?pageSize={{pageSize}}&pageNumber={{pageNumber}}';
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
            'pageSize',
            'pageNumber',
        ];
    }

    static use(args?: GetAppendedEventsArguments): [QueryResultWithState<PagedQueryResult>, PerformQuery<GetAppendedEventsArguments>] {
        return useQuery<PagedQueryResult, GetAppendedEvents, GetAppendedEventsArguments>(GetAppendedEvents, args);
    }
}
