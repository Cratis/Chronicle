/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { QueryFor, QueryResultWithState, useQuery, PerformQuery } from '@aksio/cratis-applications-frontend/queries';
import { AccountHolderWithAccounts } from './AccountHolderWithAccounts';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/accountholders/with-accounts');

export class AllAccountHoldersWithAccounts extends QueryFor<AccountHolderWithAccounts[]> {
    readonly route: string = '/api/accountholders/with-accounts';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: AccountHolderWithAccounts[] = [];

    constructor() {
        super(AccountHolderWithAccounts, true);
    }

    get requestArguments(): string[] {
        return [
        ];
    }

    static use(): [QueryResultWithState<AccountHolderWithAccounts[]>, PerformQuery] {
        return useQuery<AccountHolderWithAccounts[], AllAccountHoldersWithAccounts>(AllAccountHoldersWithAccounts);
    }
}
