/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { QueryFor, QueryResultWithState, useQuery, PerformQuery } from '@aksio/applications/queries';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/{{microserviceId}}/{{tenantId}}/sequence/{{eventSequenceId}}/tail-sequence-number/observer/{{observerId}}');

export interface TailForObserverArguments {
    eventSequenceId: string;
    microserviceId: string;
    tenantId: string;
    observerId: string;
}
export class TailForObserver extends QueryFor<number, TailForObserverArguments> {
    readonly route: string = '/api/events/store/{{microserviceId}}/{{tenantId}}/sequence/{{eventSequenceId}}/tail-sequence-number/observer/{{observerId}}';
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
            'observerId',
        ];
    }

    static use(args?: TailForObserverArguments): [QueryResultWithState<number>, PerformQuery<TailForObserverArguments>] {
        return useQuery<number, TailForObserver, TailForObserverArguments>(TailForObserver, args);
    }
}
