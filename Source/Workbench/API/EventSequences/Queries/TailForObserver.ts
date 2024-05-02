/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { QueryFor, QueryResultWithState, useQuery, PerformQuery } from 'Infrastructure/queries';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/{eventStore}/{namespace}/sequence/{eventSequenceId}/tail-sequence-number/observer/{observerId}/tail-sequence-number/observer/{observerId}');

export interface TailForObserverArguments {
    eventStore: string;
    namespace: string;
    eventSequenceId: string;
    observerId: string;
}
export class TailForObserver extends QueryFor<number, TailForObserverArguments> {
    readonly route: string = '/api/events/store/{eventStore}/{namespace}/sequence/{eventSequenceId}/tail-sequence-number/observer/{observerId}/tail-sequence-number/observer/{observerId}';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: number = {} as any;

    constructor() {
        super(Number, false);
    }

    get requestArguments(): string[] {
        return [
            'eventStore',
            'namespace',
            'eventSequenceId',
            'observerId',
        ];
    }

    static use(args?: TailForObserverArguments): [QueryResultWithState<number>, PerformQuery<TailForObserverArguments>] {
        return useQuery<number, TailForObserver, TailForObserverArguments>(TailForObserver, args);
    }
}
