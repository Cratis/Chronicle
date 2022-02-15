/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { Command } from '@aksio/cratis-applications-frontend/commands';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/accounts/debit/{{accountId}}/deposit/{{amount}}');

export class DepositToAccount extends Command {
    readonly route: string = '/api/accounts/debit/{{accountId}}/deposit/{{amount}}';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;

    get requestArguments(): string[] {
        return [
            'accountId',
            'amount',
        ];
    }

    accountId!: string;
    amount!: number;
}
