/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { QueryFor, QueryResultWithState, useQuery, PerformQuery } from 'Infrastructure/queries';
import { PagedQueryResult`1 } from '../../PagedQueryResult`1';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/{eventStore}/{namespace}/sequence/{eventSequenceId}/range/range');

export interface GetAppendedEventsInRangeArguments {
    eventStore: string;
    namespace: string;
    eventSequenceId: string;
    fromSequenceNumber: number;
    toSequenceNumber: number;
    eventSourceId?: string;
}
export class GetAppendedEventsInRange extends QueryFor<PagedQueryResult`1, GetAppendedEventsInRangeArguments> {
    readonly route: string = '/api/events/store/{eventStore}/{namespace}/sequence/{eventSequenceId}/range/range';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: PagedQueryResult`1 = {} as any;

    constructor() {
        super(PagedQueryResult`1, false);
    }

    get requestArguments(): string[] {
        return [
            'eventStore',
            'namespace',
            'eventSequenceId',
            'fromSequenceNumber',
            'toSequenceNumber',
            'eventSourceId',
        ];
    }

    static use(args?: GetAppendedEventsInRangeArguments): [QueryResultWithState<PagedQueryResult`1>, PerformQuery<GetAppendedEventsInRangeArguments>] {
        return useQuery<PagedQueryResult`1, GetAppendedEventsInRange, GetAppendedEventsInRangeArguments>(GetAppendedEventsInRange, args);
    }
}
