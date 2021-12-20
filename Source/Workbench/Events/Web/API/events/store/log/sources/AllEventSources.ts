/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { QueryFor, QueryResult, useQuery, PerformQuery } from '@aksio/frontend/queries';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/log/sources');

export class AllEventSources extends QueryFor<string[]> {
    readonly route: string = '/api/events/store/log/sources';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: string[] = [];
    readonly requiresArguments: boolean = false;

    static use(): [QueryResult<string[]>, PerformQuery] {
        return useQuery<string[], AllEventSources>(AllEventSources);
    }
}
