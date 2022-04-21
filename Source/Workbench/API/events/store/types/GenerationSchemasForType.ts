/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { QueryFor, QueryResult, useQuery, PerformQuery } from '@aksio/cratis-applications-frontend/queries';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/types/schemas/{{eventTypeId}}?microserviceId={{microserviceId}}');

export interface GenerationSchemasForTypeArguments {
    microserviceId: string;
    eventTypeId: string;
}
export class GenerationSchemasForType extends QueryFor<any[], GenerationSchemasForTypeArguments> {
    readonly route: string = '/api/events/store/types/schemas/{{eventTypeId}}?microserviceId={{microserviceId}}';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: any[] = [];

    get requestArguments(): string[] {
        return [
            'microserviceId',
            'eventTypeId',
        ];
    }

    static use(args?: GenerationSchemasForTypeArguments): [QueryResult<any[]>, PerformQuery<GenerationSchemasForTypeArguments>] {
        return useQuery<any[], GenerationSchemasForType, GenerationSchemasForTypeArguments>(GenerationSchemasForType, args);
    }
}
