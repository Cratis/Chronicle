/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { QueryFor, QueryResult, useQuery, PerformQuery } from '@aksio/frontend/queries';
import { EventLogInformation } from './EventLogInformation';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/logs');

export class AllEventLogs extends QueryFor<EventLogInformation[]> {
    readonly route: string = '/api/events/store/logs';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: EventLogInformation[] = [];
    readonly requiresArguments: boolean = false;

    static use(): [QueryResult<EventLogInformation[]>, PerformQuery] {
        return useQuery<EventLogInformation[], AllEventLogs>(AllEventLogs);
    }
}
