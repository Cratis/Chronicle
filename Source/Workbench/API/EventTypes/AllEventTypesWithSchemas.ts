// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { QueryFor, QueryResultWithState, useQuery, PerformQuery } from 'Infrastructure/queries';
import { EventTypeWithSchemas } from './EventTypeWithSchemas';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/{eventStore}/types/schemas/schemas');

export interface AllEventTypesWithSchemasArguments {
    eventStore: string;
}
export class AllEventTypesWithSchemas extends QueryFor<EventTypeWithSchemas[], AllEventTypesWithSchemasArguments> {
    readonly route: string = '/api/events/store/{eventStore}/types/schemas/schemas';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: EventTypeWithSchemas[] = [];

    constructor() {
        super(EventTypeWithSchemas, true);
    }

    get requestArguments(): string[] {
        return [
            'eventStore',
        ];
    }

    static use(args?: AllEventTypesWithSchemasArguments): [QueryResultWithState<EventTypeWithSchemas[]>, PerformQuery<AllEventTypesWithSchemasArguments>] {
        return useQuery<EventTypeWithSchemas[], AllEventTypesWithSchemas, AllEventTypesWithSchemasArguments>(AllEventTypesWithSchemas, args);
    }
}
