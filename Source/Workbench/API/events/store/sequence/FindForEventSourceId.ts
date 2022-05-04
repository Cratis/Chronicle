/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { QueryFor, QueryResult, useQuery, PerformQuery } from '@aksio/cratis-applications-frontend/queries';
import { AppendedEvent } from './AppendedEvent';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/sequence/{{eventSequenceId}}/{{eventSourceId}}');

export interface FindForEventSourceIdArguments {
    eventSequenceId: string;
    eventSourceId: string;
}
export class FindForEventSourceId extends QueryFor<AppendedEvent[], FindForEventSourceIdArguments> {
    readonly route: string = '/api/events/store/sequence/{{eventSequenceId}}/{{eventSourceId}}';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: AppendedEvent[] = [];

    get requestArguments(): string[] {
        return [
            'eventSequenceId',
            'eventSourceId',
        ];
    }

    static use(args?: FindForEventSourceIdArguments): [QueryResult<AppendedEvent[]>, PerformQuery<FindForEventSourceIdArguments>] {
        return useQuery<AppendedEvent[], FindForEventSourceId, FindForEventSourceIdArguments>(FindForEventSourceId, args);
    }
}
