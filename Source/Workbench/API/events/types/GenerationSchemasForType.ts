/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { QueryFor, QueryResult, useQuery, PerformQuery } from '@aksio/cratis-applications-frontend/queries';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/types/schemas/{{eventTypeId}}');

export interface GenerationSchemasForTypeArguments {
    eventTypeId: string;
}
export class GenerationSchemasForType extends QueryFor<string[], GenerationSchemasForTypeArguments> {
    readonly route: string = '/api/events/types/schemas/{{eventTypeId}}';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: string[] = [];
    readonly requiresArguments: boolean = true;

    static use(args?: GenerationSchemasForTypeArguments): [QueryResult<string[]>, PerformQuery<GenerationSchemasForTypeArguments>] {
        return useQuery<string[], GenerationSchemasForType, GenerationSchemasForTypeArguments>(GenerationSchemasForType, args);
    }
}
