/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { QueryFor, QueryResultWithState, useQuery, PerformQuery } from 'Infrastructure/queries';
import { EventType } from './EventType';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/{eventStore}/types');

export interface AllEventTypesArguments {
    eventStore: string;
}
export class AllEventTypes extends QueryFor<EventType[], AllEventTypesArguments> {
    readonly route: string = '/api/events/store/{eventStore}/types';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: EventType[] = [];

    constructor() {
        super(EventType, true);
    }

    get requestArguments(): string[] {
        return [
            'eventStore',
        ];
    }

    static use(args?: AllEventTypesArguments): [QueryResultWithState<EventType[]>, PerformQuery<AllEventTypesArguments>] {
        return useQuery<EventType[], AllEventTypes, AllEventTypesArguments>(AllEventTypes, args);
    }
}
