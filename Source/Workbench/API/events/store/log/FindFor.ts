/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { QueryFor, QueryResult, useQuery, PerformQuery } from '@aksio/frontend/queries';
import { Event } from './Event';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/log/{{eventLogId}}');

export interface FindForArguments {
    eventLogId: string;
}
export class FindFor extends QueryFor<Event[], FindForArguments> {
    readonly route: string = '/api/events/store/log/{{eventLogId}}';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: Event[] = [];
    readonly requiresArguments: boolean = true;

    static use(args?: FindForArguments): [QueryResult<Event[]>, PerformQuery<FindForArguments>] {
        return useQuery<Event[], FindFor, FindForArguments>(FindFor, args);
    }
}
