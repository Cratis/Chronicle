/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { QueryFor, QueryResult, useQuery, PerformQuery } from '@aksio/cratis-applications-frontend/queries';
import { AccountHolder } from './AccountHolder';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/accountholders');

export class AllAccountHolders extends QueryFor<AccountHolder[]> {
    readonly route: string = '/api/accountholders';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: AccountHolder[] = [];
    readonly requiresArguments: boolean = false;

    static use(): [QueryResult<AccountHolder[]>, PerformQuery] {
        return useQuery<AccountHolder[], AllAccountHolders>(AllAccountHolders);
    }
}
