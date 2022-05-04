/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { QueryFor, QueryResult, useQuery, PerformQuery } from '@aksio/cratis-applications-frontend/queries';
import { EventTypeInformation } from './EventTypeInformation';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/types?microserviceId={{microserviceId}}');

export interface AllEventTypesArguments {
    microserviceId: string;
}
export class AllEventTypes extends QueryFor<EventTypeInformation[], AllEventTypesArguments> {
    readonly route: string = '/api/events/store/types?microserviceId={{microserviceId}}';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: EventTypeInformation[] = [];

    get requestArguments(): string[] {
        return [
            'microserviceId',
        ];
    }

    static use(args?: AllEventTypesArguments): [QueryResult<EventTypeInformation[]>, PerformQuery<AllEventTypesArguments>] {
        return useQuery<EventTypeInformation[], AllEventTypes, AllEventTypesArguments>(AllEventTypes, args);
    }
}
