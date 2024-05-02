/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { QueryFor, QueryResultWithState, useQuery, PerformQuery } from 'Infrastructure/queries';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/{eventStore}/{namespace}/sequence/{eventSequenceId}/next-sequence-number/next-sequence-number');

export interface NextArguments {
    eventStore: string;
    namespace: string;
    eventSequenceId: string;
}
export class Next extends QueryFor<number, NextArguments> {
    readonly route: string = '/api/events/store/{eventStore}/{namespace}/sequence/{eventSequenceId}/next-sequence-number/next-sequence-number';
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

    static use(args?: NextArguments): [QueryResultWithState<number>, PerformQuery<NextArguments>] {
        return useQuery<number, Next, NextArguments>(Next, args);
    }
}
