/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { QueryFor, QueryResult, useQuery, PerformQuery } from '@aksio/cratis-applications-frontend/queries';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/types/schemas/{{eventTypeId}}');

export interface GenerationSchemasForTypeArguments {
    eventTypeId: string;
}
export class GenerationSchemasForType extends QueryFor<any[], GenerationSchemasForTypeArguments> {
    readonly route: string = '/api/events/types/schemas/{{eventTypeId}}';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: any[] = [];
    readonly requiresArguments: boolean = true;

    static use(args?: GenerationSchemasForTypeArguments): [QueryResult<any[]>, PerformQuery<GenerationSchemasForTypeArguments>] {
        return useQuery<any[], GenerationSchemasForType, GenerationSchemasForTypeArguments>(GenerationSchemasForType, args);
    }
}
