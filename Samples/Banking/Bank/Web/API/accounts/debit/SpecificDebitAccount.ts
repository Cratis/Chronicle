/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { ObservableQueryFor, QueryResultWithState, useObservableQuery } from '@aksio/cratis-applications-frontend/queries';
import { DebitAccount } from './DebitAccount';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/accounts/debit/{{accountId}}');

export interface SpecificDebitAccountArguments {
    accountId: string;
}
export class SpecificDebitAccount extends ObservableQueryFor<DebitAccount, SpecificDebitAccountArguments> {
    readonly route: string = '/api/accounts/debit/{{accountId}}';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: DebitAccount = {} as any;

    constructor() {
        super(DebitAccount, false);
    }

    get requestArguments(): string[] {
        return [
            'accountId',
        ];
    }

    static use(args?: SpecificDebitAccountArguments): [QueryResultWithState<DebitAccount>] {
        return useObservableQuery<DebitAccount, SpecificDebitAccount, SpecificDebitAccountArguments>(SpecificDebitAccount, args);
    }
}
