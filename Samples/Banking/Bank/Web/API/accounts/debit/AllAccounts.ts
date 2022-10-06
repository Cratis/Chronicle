/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { ObservableQueryFor, QueryResultWithState, useObservableQuery } from '@aksio/cratis-applications-frontend/queries';
import { DebitAccount } from './DebitAccount';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/accounts/debit');

export class AllAccounts extends ObservableQueryFor<DebitAccount[]> {
    readonly route: string = '/api/accounts/debit';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: DebitAccount[] = [];

    get requestArguments(): string[] {
        return [
        ];
    }

    static use(): [QueryResultWithState<DebitAccount[]>] {
        return useObservableQuery<DebitAccount[], AllAccounts>(AllAccounts);
    }
}
