/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

/* eslint-disable sort-imports */
// eslint-disable-next-line header/header
import { QueryFor, QueryResultWithState } from '@cratis/applications/queries';
import { useQuery, PerformQuery } from '@cratis/applications.react/queries';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/{{eventStore}}/{{namespace}}/sequence/{{eventSequenceId}}/tail-sequence-number');


export interface TailArguments {
    eventStore: string;
    namespace: string;
    eventSequenceId: string;
}

export class Tail extends QueryFor<number, TailArguments> {
    readonly route: string = '/api/event-store/{eventStore}/{namespace}/sequence/{eventSequenceId}/tail-sequence-number';
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
        const [result, perform] = useQuery<number, Tail, TailArguments>(Tail, args);
        return [result, perform];
    }
}
