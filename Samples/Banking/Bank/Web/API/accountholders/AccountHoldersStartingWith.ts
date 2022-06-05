/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { QueryFor, QueryResult, useQuery, PerformQuery } from '@aksio/cratis-applications-frontend/queries';
import { AccountHolder } from './AccountHolder';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/accountholders/starting-with?filter={{filter}}');

export interface AccountHoldersStartingWithArguments {
    filter?: string;
}
export class AccountHoldersStartingWith extends QueryFor<AccountHolder[], AccountHoldersStartingWithArguments> {
    readonly route: string = '/api/accountholders/starting-with?filter={{filter}}';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: AccountHolder[] = [];

    get requestArguments(): string[] {
        return [
            'filter',
        ];
    }

    static use(args?: AccountHoldersStartingWithArguments): [QueryResult<AccountHolder[]>, PerformQuery<AccountHoldersStartingWithArguments>] {
        return useQuery<AccountHolder[], AccountHoldersStartingWith, AccountHoldersStartingWithArguments>(AccountHoldersStartingWith, args);
    }
}
