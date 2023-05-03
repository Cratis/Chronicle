/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { QueryFor, QueryResultWithState, useQuery, PerformQuery } from '@aksio/cratis-applications-frontend/queries';
import { EventTypeWithSchemas } from './EventTypeWithSchemas';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/{{microserviceId}}/types/schemas');

export interface AllEventTypesWithSchemasArguments {
    microserviceId: string;
}
export class AllEventTypesWithSchemas extends QueryFor<EventTypeWithSchemas[], AllEventTypesWithSchemasArguments> {
    readonly route: string = '/api/events/store/{{microserviceId}}/types/schemas';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: EventTypeWithSchemas[] = [];

    constructor() {
        super(EventTypeWithSchemas, true);
    }

    get requestArguments(): string[] {
        return [
            'microserviceId',
        ];
    }

    static use(args?: AllEventTypesWithSchemasArguments): [QueryResultWithState<EventTypeWithSchemas[]>, PerformQuery<AllEventTypesWithSchemasArguments>] {
        return useQuery<EventTypeWithSchemas[], AllEventTypesWithSchemas, AllEventTypesWithSchemasArguments>(AllEventTypesWithSchemas, args);
    }
}
