/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { QueryFor, QueryResult, useQuery, PerformQuery } from '@aksio/frontend/queries';
import { ClientObservable } from './ClientObservable';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/compliance/gdpr/people');

export class AllPeople extends QueryFor<ClientObservable> {
    readonly route: string = '/api/compliance/gdpr/people';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: ClientObservable = {} as any;
    readonly requiresArguments: boolean = false;

    static use(): [QueryResult<ClientObservable>, PerformQuery] {
        return useQuery<ClientObservable, AllPeople>(AllPeople);
    }
}
