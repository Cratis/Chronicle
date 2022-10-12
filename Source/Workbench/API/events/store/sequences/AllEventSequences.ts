/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { QueryFor, QueryResultWithState, useQuery, PerformQuery } from '@aksio/cratis-applications-frontend/queries';
import { EventSequenceInformation } from './EventSequenceInformation';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/sequences');

export class AllEventSequences extends QueryFor<EventSequenceInformation[]> {
    readonly route: string = '/api/events/store/sequences';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: EventSequenceInformation[] = [];

    constructor() {
        super(EventSequenceInformation, true);
    }

    get requestArguments(): string[] {
        return [
        ];
    }

    static use(): [QueryResultWithState<EventSequenceInformation[]>, PerformQuery] {
        return useQuery<EventSequenceInformation[], AllEventSequences>(AllEventSequences);
    }
}
