/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { QueryFor, QueryResultWithState, useQuery, PerformQuery } from '@aksio/cratis-applications-frontend/queries';
import { AppendedEventWithJsonAsContent } from '../sequence/AppendedEventWithJsonAsContent';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/{{microserviceId}}/{{tenantId}}/sequence/{{eventSequenceId}}');

export interface FindForArguments {
    eventSequenceId: string;
    microserviceId: string;
    tenantId: string;
}
export class FindFor extends QueryFor<AppendedEventWithJsonAsContent[], FindForArguments> {
    readonly route: string = '/api/events/store/{{microserviceId}}/{{tenantId}}/sequence/{{eventSequenceId}}';
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
        ];
    }

    static use(args?: FindForArguments): [QueryResultWithState<AppendedEventWithJsonAsContent[]>, PerformQuery<FindForArguments>] {
        return useQuery<AppendedEventWithJsonAsContent[], FindFor, FindForArguments>(FindFor, args);
    }
}
