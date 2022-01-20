/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { QueryFor, QueryResult, useQuery, PerformQuery } from '@aksio/cratis-applications-frontend/queries';
import { DebitAccount } from './DebitAccount';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/accounts/debit/starting-with?filter={{filter}}');

export interface StartingWithArguments {
    filter?: string;
}
export class StartingWith extends QueryFor<DebitAccount[], StartingWithArguments> {
    readonly route: string = '/api/accounts/debit/starting-with?filter={{filter}}';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: DebitAccount[] = [];
    readonly requiresArguments: boolean = true;

    static use(args?: StartingWithArguments): [QueryResult<DebitAccount[]>, PerformQuery<StartingWithArguments>] {
        return useQuery<DebitAccount[], StartingWith, StartingWithArguments>(StartingWith, args);
    }
}
