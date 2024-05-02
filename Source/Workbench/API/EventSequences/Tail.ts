/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { QueryFor, QueryResultWithState, useQuery, PerformQuery } from 'Infrastructure/queries';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/{eventStore}/{namespace}/sequence/{eventSequenceId}/tail-sequence-number/tail-sequence-number');

export interface TailArguments {
    eventStore: string;
    namespace: string;
    eventSequenceId: string;
}
export class Tail extends QueryFor<number, TailArguments> {
    readonly route: string = '/api/events/store/{eventStore}/{namespace}/sequence/{eventSequenceId}/tail-sequence-number/tail-sequence-number';
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
        ];
    }

    static use(args?: TailArguments): [QueryResultWithState<number>, PerformQuery<TailArguments>] {
        return useQuery<number, Tail, TailArguments>(Tail, args);
    }
}
