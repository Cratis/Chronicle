/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { QueryFor, QueryResult, useQuery, PerformQuery } from '@aksio/cratis-applications-frontend/queries';
import { EventSequenceInformation } from './EventSequenceInformation';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/sequences');

export class AllEventSequences extends QueryFor<EventSequenceInformation[]> {
    readonly route: string = '/api/events/store/sequences';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: EventSequenceInformation[] = [];

    get requestArguments(): string[] {
        return [
        ];
    }

    static use(): [QueryResult<EventSequenceInformation[]>, PerformQuery] {
        return useQuery<EventSequenceInformation[], AllEventSequences>(AllEventSequences);
    }
}
