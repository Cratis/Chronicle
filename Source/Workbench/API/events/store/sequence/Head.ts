/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { QueryFor, QueryResultWithState, useQuery, PerformQuery } from '@aksio/cratis-applications-frontend/queries';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/{microserviceId}/{tenantId}/sequence/{{eventSequenceId}}/head?microserviceId={{microserviceId}}&tenantId={{tenantId}}');

export interface HeadArguments {
    eventSequenceId: string;
    microserviceId: string;
    tenantId: string;
}
export class Head extends QueryFor<number, HeadArguments> {
    readonly route: string = '/api/events/store/{microserviceId}/{tenantId}/sequence/{{eventSequenceId}}/head?microserviceId={{microserviceId}}&tenantId={{tenantId}}';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: number = {} as any;

    constructor() {
        super(Number, false);
    }

    get requestArguments(): string[] {
        return [
            'eventSequenceId',
            'microserviceId',
            'tenantId',
        ];
    }

    static use(args?: HeadArguments): [QueryResultWithState<number>, PerformQuery<HeadArguments>] {
        return useQuery<number, Head, HeadArguments>(Head, args);
    }
}
