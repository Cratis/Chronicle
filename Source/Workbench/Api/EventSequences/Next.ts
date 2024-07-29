/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

// eslint-disable-next-line header/header
import { QueryFor, QueryResultWithState } from '@cratis/applications/queries';
import { useQuery, PerformQuery, SetSorting} from '@cratis/applications.react/queries';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/{eventStore}/{namespace}/sequence/{eventSequenceId}/next-sequence-number');


export interface NextArguments {
    eventStore: string;
    namespace: string;
    eventSequenceId: string;
}

export class Next extends QueryFor<number, NextArguments> {
    readonly route: string = '/api/events/store/{eventStore}/{namespace}/sequence/{eventSequenceId}/next-sequence-number';
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


    static use(args?: NextArguments, sorting?: Sorting): [QueryResultWithState<number[]>, PerformQuery<NextArguments>] {
        return useQuery<number[], Next, NextArguments>(Next, args, sorting);
    }
}
