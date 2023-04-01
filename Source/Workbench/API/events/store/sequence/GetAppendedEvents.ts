/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { QueryFor, QueryResultWithState, useQuery, PerformQuery } from '@aksio/cratis-applications-frontend/queries';
import { AppendedEvents } from '../sequence/AppendedEvents';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/{{microserviceId}}/{{tenantId}}/sequence/{{eventSequenceId}}?pageSize={{pageSize}}&pageNumber={{pageNumber}}');

export interface GetAppendedEventsArguments {
    eventSequenceId: string;
    microserviceId: string;
    tenantId: string;
    pageSize: number;
    pageNumber: number;
}
export class GetAppendedEvents extends QueryFor<AppendedEvents, GetAppendedEventsArguments> {
    readonly route: string = '/api/events/store/{{microserviceId}}/{{tenantId}}/sequence/{{eventSequenceId}}?pageSize={{pageSize}}&pageNumber={{pageNumber}}';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: AppendedEvents = {} as any;

    constructor() {
        super(AppendedEvents, false);
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

    static use(args?: GetAppendedEventsArguments): [QueryResultWithState<AppendedEvents>, PerformQuery<GetAppendedEventsArguments>] {
        return useQuery<AppendedEvents, GetAppendedEvents, GetAppendedEventsArguments>(GetAppendedEvents, args);
    }
}
