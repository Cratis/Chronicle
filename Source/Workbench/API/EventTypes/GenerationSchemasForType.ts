/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { QueryFor, QueryResultWithState, useQuery, PerformQuery } from 'Infrastructure/queries';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/{eventStore}/types/schemas/{eventTypeId}/schemas/{eventTypeId}');

export interface GenerationSchemasForTypeArguments {
    eventStore: string;
    eventTypeId: string;
}
export class GenerationSchemasForType extends QueryFor<any[], GenerationSchemasForTypeArguments> {
    readonly route: string = '/api/events/store/{eventStore}/types/schemas/{eventTypeId}/schemas/{eventTypeId}';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: any[] = [];

    constructor() {
        super(Object, true);
    }

    get requestArguments(): string[] {
        return [
            'eventStore',
            'eventTypeId',
        ];
    }

    static use(args?: GenerationSchemasForTypeArguments): [QueryResultWithState<any[]>, PerformQuery<GenerationSchemasForTypeArguments>] {
        return useQuery<any[], GenerationSchemasForType, GenerationSchemasForTypeArguments>(GenerationSchemasForType, args);
    }
}
