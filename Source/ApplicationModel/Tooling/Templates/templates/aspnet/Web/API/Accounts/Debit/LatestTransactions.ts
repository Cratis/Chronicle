/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { QueryFor, QueryResult, useQuery, PerformQuery } from '@aksio/cratis-applications-frontend/queries';
import { DebitAccountLatestTransactions } from './DebitAccountLatestTransactions';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/accounts/debit/latest-transactions/{{accountId}}');

export interface LatestTransactionsArguments {
    accountId: string;
}
export class LatestTransactions extends QueryFor<DebitAccountLatestTransactions, LatestTransactionsArguments> {
    readonly route: string = '/api/accounts/debit/latest-transactions/{{accountId}}';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: DebitAccountLatestTransactions = {} as any;

    get requestArguments(): string[] {
        return [
            'accountId',
        ];
    }

    static use(args?: LatestTransactionsArguments): [QueryResult<DebitAccountLatestTransactions>, PerformQuery<LatestTransactionsArguments>] {
        return useQuery<DebitAccountLatestTransactions, LatestTransactions, LatestTransactionsArguments>(LatestTransactions, args);
    }
}
