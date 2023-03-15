/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { QueryFor, QueryResultWithState, useQuery, PerformQuery } from '@aksio/cratis-applications-frontend/queries';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/{{microserviceId}}/{{tenantId}}/sequence/{{eventSequenceId}}/next-sequence-number');

export interface NextArguments {
    eventSequenceId: string;
    microserviceId: string;
    tenantId: string;
}
export class Next extends QueryFor<number, NextArguments> {
    readonly route: string = '/api/events/store/{{microserviceId}}/{{tenantId}}/sequence/{{eventSequenceId}}/next-sequence-number';
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

    static use(args?: NextArguments): [QueryResultWithState<number>, PerformQuery<NextArguments>] {
        return useQuery<number, Next, NextArguments>(Next, args);
    }
}
