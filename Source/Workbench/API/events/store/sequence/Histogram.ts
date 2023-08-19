/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { QueryFor, QueryResultWithState, useQuery, PerformQuery } from '@aksio/applications/queries';
import { EventHistogramEntry } from './EventHistogramEntry';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/{microserviceId}/{tenantId}/sequence/{eventSequenceId}/histogram');

export class Histogram extends QueryFor<EventHistogramEntry[]> {
    readonly route: string = '/api/events/store/{microserviceId}/{tenantId}/sequence/{eventSequenceId}/histogram';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: EventHistogramEntry[] = [];

    constructor() {
        super(EventHistogramEntry, true);
    }

    get requestArguments(): string[] {
        return [
        ];
    }

    static use(): [QueryResultWithState<EventHistogramEntry[]>, PerformQuery] {
        return useQuery<EventHistogramEntry[], Histogram>(Histogram);
    }
}
