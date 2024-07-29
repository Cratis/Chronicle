/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

// eslint-disable-next-line header/header
import { QueryFor, QueryResultWithState } from '@cratis/applications/queries';
import { useQuery, PerformQuery, SetSorting} from '@cratis/applications.react/queries';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/{eventStore}/{namespace}/sequence/{eventSequenceId}/tail-sequence-number/observer/{observerId}');


export interface TailForObserverArguments {
    eventStore: string;
    namespace: string;
    eventSequenceId: string;
    observerId: string;
}

export class TailForObserver extends QueryFor<number, TailForObserverArguments> {
    readonly route: string = '/api/events/store/{eventStore}/{namespace}/sequence/{eventSequenceId}/tail-sequence-number/observer/{observerId}';
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


    static use(args?: TailForObserverArguments, sorting?: Sorting): [QueryResultWithState<number[]>, PerformQuery<TailForObserverArguments>] {
        return useQuery<number[], TailForObserver, TailForObserverArguments>(TailForObserver, args, sorting);
    }
}
