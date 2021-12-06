/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { QueryFor, QueryResult, useQuery, PerformQuery } from '@aksio/frontend/queries';
import { EventType } from './EventType';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/types');

export class AllEventTypes extends QueryFor<EventType[]> {
    readonly route: string = '/api/events/types';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: EventType[] = [];
    readonly requiresArguments: boolean = false;

    static use(): [QueryResult<EventType[]>, PerformQuery] {
        return useQuery<EventType[], AllEventTypes>(AllEventTypes);
    }
}
