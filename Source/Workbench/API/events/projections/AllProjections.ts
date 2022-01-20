/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { QueryFor, QueryResult, useQuery, PerformQuery } from '@aksio/frontend/queries';
import { ClientObservable } from './ClientObservable';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/projections');

export class AllProjections extends QueryFor<ClientObservable> {
    readonly route: string = '/api/events/projections';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: ClientObservable = {} as any;
    readonly requiresArguments: boolean = false;

    static use(): [QueryResult<ClientObservable>, PerformQuery] {
        return useQuery<ClientObservable, AllProjections>(AllProjections);
    }
}
