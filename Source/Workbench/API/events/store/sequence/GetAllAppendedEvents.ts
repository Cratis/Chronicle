/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { QueryFor, QueryResultWithState, useQuery, PerformQuery } from '@aksio/applications/queries';
import { AppendedEventWithJsonAsContent } from './AppendedEventWithJsonAsContent';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/{{microserviceId}}/{{tenantId}}/sequence/{{eventSequenceId}}/all?eventSourceId={{eventSourceId}}&eventTypes={{eventTypes}}');

export interface GetAllAppendedEventsArguments {
    eventSequenceId: string;
    microserviceId: string;
    tenantId: string;
    eventSourceId: string;
    eventTypes: any;
}
export class GetAllAppendedEvents extends QueryFor<AppendedEventWithJsonAsContent[], GetAllAppendedEventsArguments> {
    readonly route: string = '/api/events/store/{{microserviceId}}/{{tenantId}}/sequence/{{eventSequenceId}}/all?eventSourceId={{eventSourceId}}&eventTypes={{eventTypes}}';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: AppendedEventWithJsonAsContent[] = [];

    constructor() {
        super(AppendedEventWithJsonAsContent, true);
    }

    get requestArguments(): string[] {
        return [
            'eventSequenceId',
            'microserviceId',
            'tenantId',
            'eventSourceId',
            'eventTypes',
        ];
    }

    static use(args?: GetAllAppendedEventsArguments): [QueryResultWithState<AppendedEventWithJsonAsContent[]>, PerformQuery<GetAllAppendedEventsArguments>] {
        return useQuery<AppendedEventWithJsonAsContent[], GetAllAppendedEvents, GetAllAppendedEventsArguments>(GetAllAppendedEvents, args);
    }
}
