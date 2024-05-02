// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { QueryFor, QueryResultWithState, useQuery, PerformQuery } from 'Infrastructure/queries';
import { AppendedEventWithJsonAsContent } from './AppendedEventWithJsonAsContent';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/{eventStore}/{namespace}/sequence/{eventSequenceId}/all/all');

export interface GetAllAppendedEventsArguments {
    eventStore: string;
    namespace: string;
    eventSequenceId: string;
    eventSourceId?: string;
}
export class GetAllAppendedEvents extends QueryFor<AppendedEventWithJsonAsContent[], GetAllAppendedEventsArguments> {
    readonly route: string = '/api/events/store/{eventStore}/{namespace}/sequence/{eventSequenceId}/all/all';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: AppendedEventWithJsonAsContent[] = [];

    constructor() {
        super(AppendedEventWithJsonAsContent, true);
    }

    get requestArguments(): string[] {
        return [
            'eventStore',
            'namespace',
            'eventSequenceId',
            'eventSourceId',
        ];
    }

    static use(args?: GetAllAppendedEventsArguments): [QueryResultWithState<AppendedEventWithJsonAsContent[]>, PerformQuery<GetAllAppendedEventsArguments>] {
        return useQuery<AppendedEventWithJsonAsContent[], GetAllAppendedEvents, GetAllAppendedEventsArguments>(GetAllAppendedEvents, args);
    }
}
