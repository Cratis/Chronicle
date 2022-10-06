/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { QueryFor, QueryResultWithState, useQuery, PerformQuery } from '@aksio/cratis-applications-frontend/queries';
import { AppendedEvent } from './AppendedEvent';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/sequence/{{eventSequenceId}}?microserviceId={{microserviceId}}&tenantId={{tenantId}}');

export interface FindForArguments {
    eventSequenceId: string;
    microserviceId: string;
    tenantId: string;
}
export class FindFor extends QueryFor<AppendedEvent[], FindForArguments> {
    readonly route: string = '/api/events/store/sequence/{{eventSequenceId}}?microserviceId={{microserviceId}}&tenantId={{tenantId}}';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: AppendedEvent[] = [];

    get requestArguments(): string[] {
        return [
            'eventSequenceId',
            'microserviceId',
            'tenantId',
        ];
    }

    static use(args?: FindForArguments): [QueryResultWithState<AppendedEvent[]>, PerformQuery<FindForArguments>] {
        return useQuery<AppendedEvent[], FindFor, FindForArguments>(FindFor, args);
    }
}
