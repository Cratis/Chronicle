/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { QueryFor, QueryResultWithState, useQuery, PerformQuery } from '@aksio/cratis-applications-frontend/queries';
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

    constructor() {
        super(EventTypeInformation, true);
    }

    get requestArguments(): string[] {
        return [
            'microserviceId',
        ];
    }

    static use(args?: AllEventTypesArguments): [QueryResultWithState<EventTypeInformation[]>, PerformQuery<AllEventTypesArguments>] {
        return useQuery<EventTypeInformation[], AllEventTypes, AllEventTypesArguments>(AllEventTypes, args);
    }
}
