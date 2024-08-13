/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

// eslint-disable-next-line header/header
import { QueryFor, QueryResultWithState } from '@cratis/applications/queries';
import { useQuery, PerformQuery } from '@cratis/applications.react/queries';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/event-store/{{eventStore}}/{{namespace}}/sequence/{{eventSequenceId}}/tail-sequence-number/observer/{{observerId}}');


export interface TailForObserverArguments {
    eventStore: string;
    namespace: string;
    eventSequenceId: string;
    observerId: string;
}

export class TailForObserver extends QueryFor<number, TailForObserverArguments> {
    readonly route: string = '/api/event-store/{eventStore}/{namespace}/sequence/{eventSequenceId}/tail-sequence-number/observer/{observerId}';
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
        const [result, perform] = useQuery<number, TailForObserver, TailForObserverArguments>(TailForObserver, args);
        return [result, perform];
    }
}
