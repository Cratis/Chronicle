/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { QueryFor, QueryResult, useQuery, PerformQuery } from '@aksio/cratis-applications-frontend/queries';
import { EventType } from './EventType';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/types?microserviceId={{microserviceId}}');

export interface AllEventTypesArguments {
    microserviceId: string;
}
export class AllEventTypes extends QueryFor<EventType[], AllEventTypesArguments> {
    readonly route: string = '/api/events/store/types?microserviceId={{microserviceId}}';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: EventType[] = [];

    get requestArguments(): string[] {
        return [
            'microserviceId',
        ];
    }

    static use(args?: AllEventTypesArguments): [QueryResult<EventType[]>, PerformQuery<AllEventTypesArguments>] {
        return useQuery<EventType[], AllEventTypes, AllEventTypesArguments>(AllEventTypes, args);
    }
}
