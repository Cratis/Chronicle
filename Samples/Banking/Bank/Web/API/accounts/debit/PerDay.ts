/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { QueryFor, QueryResultWithState, useQuery, PerformQuery } from '@aksio/applications/queries';
import { DebitAccountsPerDay } from './DebitAccountsPerDay';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/accounts/debit/accounts-per-day');

export class PerDay extends QueryFor<DebitAccountsPerDay[]> {
    readonly route: string = '/api/accounts/debit/accounts-per-day';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: DebitAccountsPerDay[] = [];

    constructor() {
        super(DebitAccountsPerDay, true);
    }

    get requestArguments(): string[] {
        return [
        ];
    }

    static use(): [QueryResultWithState<DebitAccountsPerDay[]>, PerformQuery] {
        return useQuery<DebitAccountsPerDay[], PerDay>(PerDay);
    }
}
