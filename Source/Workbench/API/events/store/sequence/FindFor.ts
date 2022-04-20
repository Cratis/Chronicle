/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { QueryFor, QueryResult, useQuery, PerformQuery } from '@aksio/cratis-applications-frontend/queries';
import { AppendedEvent } from './AppendedEvent';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/sequence/{{eventSequenceId}}?microserviceId={{microserviceId}}');

export interface FindForArguments {
    eventSequenceId: string;
    microserviceId: string;
}
export class FindFor extends QueryFor<AppendedEvent[], FindForArguments> {
    readonly route: string = '/api/events/store/sequence/{{eventSequenceId}}?microserviceId={{microserviceId}}';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: AppendedEvent[] = [];
    readonly requiresArguments: boolean = true;

    static use(args?: FindForArguments): [QueryResult<AppendedEvent[]>, PerformQuery<FindForArguments>] {
        return useQuery<AppendedEvent[], FindFor, FindForArguments>(FindFor, args);
    }
}
